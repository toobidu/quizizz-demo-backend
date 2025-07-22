# Hướng Dẫn Tích Hợp Frontend với API và Socket

## 1. Thiết Lập Kết Nối

### 1.1. Kết Nối API

```javascript
// Cấu hình API
const API_BASE_URL = 'http://your-server-url/api';

// Hàm gọi API chung
async function callApi(endpoint, method = 'GET', data = null) {
  const url = `${API_BASE_URL}${endpoint}`;
  const options = {
    method,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': localStorage.getItem('token') ? `Bearer ${localStorage.getItem('token')}` : ''
    }
  };

  if (data && (method === 'POST' || method === 'PUT')) {
    options.body = JSON.stringify(data);
  }

  try {
    const response = await fetch(url, options);
    const result = await response.json();
    
    if (!response.ok) {
      throw new Error(result.message || 'Lỗi khi gọi API');
    }
    
    return result;
  } catch (error) {
    console.error(`Lỗi API (${endpoint}):`, error);
    throw error;
  }
}
```

### 1.2. Kết Nối WebSocket

```javascript
class GameSocketManager {
  constructor() {
    this.socket = null;
    this.isConnected = false;
    this.eventHandlers = {};
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 5;
    this.reconnectDelay = 2000; // 2 giây
  }

  // Kết nối đến WebSocket server
  connect() {
    return new Promise((resolve, reject) => {
      try {
        this.socket = new WebSocket('ws://your-server-url/ws');
        
        this.socket.onopen = () => {
          console.log('Kết nối WebSocket thành công');
          this.isConnected = true;
          this.reconnectAttempts = 0;
          resolve();
        };
        
        this.socket.onmessage = (event) => {
          try {
            const message = JSON.parse(event.data);
            this.handleMessage(message);
          } catch (error) {
            console.error('Lỗi xử lý tin nhắn:', error);
          }
        };
        
        this.socket.onclose = () => {
          console.log('Kết nối WebSocket đã đóng');
          this.isConnected = false;
          this.attemptReconnect();
        };
        
        this.socket.onerror = (error) => {
          console.error('Lỗi WebSocket:', error);
          reject(error);
        };
      } catch (error) {
        console.error('Lỗi khi tạo kết nối WebSocket:', error);
        reject(error);
      }
    });
  }

  // Xử lý tin nhắn nhận được
  handleMessage(message) {
    const { type, data } = message;
    
    if (this.eventHandlers[type]) {
      this.eventHandlers[type].forEach(handler => {
        try {
          handler(data);
        } catch (error) {
          console.error(`Lỗi xử lý sự kiện ${type}:`, error);
        }
      });
    }
  }

  // Đăng ký handler cho sự kiện
  on(eventType, handler) {
    if (!this.eventHandlers[eventType]) {
      this.eventHandlers[eventType] = [];
    }
    this.eventHandlers[eventType].push(handler);
  }

  // Hủy đăng ký handler
  off(eventType, handler) {
    if (this.eventHandlers[eventType]) {
      this.eventHandlers[eventType] = this.eventHandlers[eventType]
        .filter(h => h !== handler);
    }
  }

  // Gửi tin nhắn
  send(type, data) {
    if (!this.isConnected) {
      console.error('Không thể gửi tin nhắn: WebSocket chưa kết nối');
      return false;
    }
    
    try {
      const message = JSON.stringify({ type, data });
      this.socket.send(message);
      return true;
    } catch (error) {
      console.error('Lỗi khi gửi tin nhắn:', error);
      return false;
    }
  }

  // Đóng kết nối
  disconnect() {
    if (this.socket) {
      this.socket.close();
      this.socket = null;
      this.isConnected = false;
    }
  }

  // Thử kết nối lại
  attemptReconnect() {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.log('Đã đạt đến số lần thử kết nối lại tối đa');
      return;
    }
    
    this.reconnectAttempts++;
    console.log(`Đang thử kết nối lại (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
    
    setTimeout(() => {
      this.connect().catch(() => {
        console.log('Kết nối lại thất bại');
      });
    }, this.reconnectDelay);
  }
}

// Khởi tạo và sử dụng
const socketManager = new GameSocketManager();
```

## 2. Quản Lý Phòng

### 2.1. Tạo Phòng

```javascript
async function createRoom(roomData) {
  try {
    const result = await callApi('/room/create', 'POST', roomData);
    
    if (result.success) {
      // Lưu thông tin phòng
      localStorage.setItem('currentRoom', JSON.stringify(result.data));
      
      // Kết nối WebSocket nếu chưa kết nối
      if (!socketManager.isConnected) {
        await socketManager.connect();
      }
      
      return result.data;
    } else {
      throw new Error(result.message);
    }
  } catch (error) {
    console.error('Lỗi khi tạo phòng:', error);
    throw error;
  }
}
```

### 2.2. Tham Gia Phòng

```javascript
async function joinRoom(joinData) {
  try {
    const result = await callApi('/room/join', 'POST', joinData);
    
    if (result.success) {
      // Lưu thông tin phòng
      localStorage.setItem('currentRoom', JSON.stringify(result.data));
      
      // Kết nối WebSocket nếu chưa kết nối
      if (!socketManager.isConnected) {
        await socketManager.connect();
      }
      
      return result.data;
    } else {
      throw new Error(result.message);
    }
  } catch (error) {
    console.error('Lỗi khi tham gia phòng:', error);
    throw error;
  }
}
```

### 2.3. Rời Phòng

```javascript
async function leaveRoom(roomCode, userId) {
  try {
    const result = await callApi('/room/leave', 'POST', { roomCode, userId });
    
    if (result.success) {
      // Xóa thông tin phòng
      localStorage.removeItem('currentRoom');
      return true;
    } else {
      throw new Error(result.message);
    }
  } catch (error) {
    console.error('Lỗi khi rời phòng:', error);
    throw error;
  }
}
```

## 3. Quản Lý Game

### 3.1. Bắt Đầu Game

```javascript
async function startGame(roomCode, hostUserId) {
  try {
    const result = await callApi('/game/start', 'POST', { roomCode, hostUserId });
    return result.success;
  } catch (error) {
    console.error('Lỗi khi bắt đầu game:', error);
    throw error;
  }
}
```

### 3.2. Xử Lý Sự Kiện Game

```javascript
// Đăng ký các handler cho sự kiện game
function setupGameEventHandlers() {
  // Sự kiện khi game bắt đầu
  socketManager.on('game-started', (data) => {
    console.log('Game đã bắt đầu:', data);
    showGameStartScreen();
  });
  
  // Sự kiện đếm ngược
  socketManager.on('countdown', (data) => {
    console.log('Đếm ngược:', data);
    updateCountdownDisplay(data);
  });
  
  // Sự kiện nhận câu hỏi
  socketManager.on('question', (data) => {
    console.log('Nhận câu hỏi:', data);
    showQuestion(data);
    startQuestionTimer(data.timeLimit);
  });
  
  // Sự kiện kết quả câu trả lời
  socketManager.on('answer-result', (data) => {
    console.log('Kết quả câu trả lời:', data);
    showAnswerResult(data);
  });
  
  // Sự kiện cập nhật tiến độ người chơi
  socketManager.on('player-progress', (data) => {
    console.log('Cập nhật tiến độ:', data);
    updateLeaderboard(data.players);
  });
  
  // Sự kiện cập nhật thời gian
  socketManager.on('game-timer-update', (data) => {
    updateGameTimer(data.remainingTime);
  });
  
  // Sự kiện kết thúc game
  socketManager.on('game-ended', (data) => {
    console.log('Game kết thúc:', data);
    showGameResults(data);
  });
}
```

### 3.3. Gửi Câu Trả Lời

```javascript
function submitAnswer(questionIndex, selectedAnswer) {
  const submitTime = Date.now();
  
  socketManager.send('submit-answer', {
    questionIndex,
    selectedAnswer,
    submitTime
  });
  
  // Hiển thị trạng thái đã gửi
  showAnswerSubmittedState();
}
```

## 4. Xử Lý UI

### 4.1. Hiển Thị Phòng Chờ

```javascript
function renderWaitingRoom(roomData) {
  const { roomCode, players, maxPlayers, host } = roomData;
  
  // Hiển thị mã phòng
  document.getElementById('room-code').textContent = roomCode;
  
  // Hiển thị danh sách người chơi
  const playersList = document.getElementById('players-list');
  playersList.innerHTML = '';
  
  players.forEach(player => {
    const playerElement = document.createElement('div');
    playerElement.classList.add('player-item');
    
    if (player.isHost) {
      playerElement.classList.add('host');
    }
    
    playerElement.innerHTML = `
      <span class="player-name">${player.username}</span>
      ${player.isHost ? '<span class="host-badge">Host</span>' : ''}
    `;
    
    playersList.appendChild(playerElement);
  });
  
  // Hiển thị số lượng người chơi
  document.getElementById('player-count').textContent = `${players.length}/${maxPlayers}`;
  
  // Hiển thị nút bắt đầu game nếu là host
  const startButton = document.getElementById('start-game-button');
  const currentUser = JSON.parse(localStorage.getItem('currentUser'));
  
  if (currentUser && currentUser.id === host) {
    startButton.style.display = 'block';
  } else {
    startButton.style.display = 'none';
  }
}
```

### 4.2. Hiển Thị Câu Hỏi

```javascript
function showQuestion(questionData) {
  const { questionId, questionIndex, totalQuestions, questionText, options, timeLimit } = questionData;
  
  // Cập nhật tiêu đề
  document.getElementById('question-number').textContent = `Câu hỏi ${questionIndex + 1}/${totalQuestions}`;
  
  // Cập nhật nội dung câu hỏi
  document.getElementById('question-text').textContent = questionText;
  
  // Hiển thị các lựa chọn
  const optionsContainer = document.getElementById('options-container');
  optionsContainer.innerHTML = '';
  
  options.forEach((option, index) => {
    const optionElement = document.createElement('div');
    optionElement.classList.add('option');
    optionElement.textContent = option;
    optionElement.dataset.value = option;
    
    optionElement.addEventListener('click', () => {
      // Bỏ chọn tất cả các lựa chọn khác
      document.querySelectorAll('.option').forEach(el => {
        el.classList.remove('selected');
      });
      
      // Chọn lựa chọn hiện tại
      optionElement.classList.add('selected');
      
      // Gửi câu trả lời
      submitAnswer(questionIndex, option);
    });
    
    optionsContainer.appendChild(optionElement);
  });
  
  // Hiển thị thời gian
  startQuestionTimer(timeLimit);
  
  // Hiển thị màn hình câu hỏi
  showScreen('question-screen');
}
```

### 4.3. Hiển Thị Kết Quả

```javascript
function showAnswerResult(resultData) {
  const { isCorrect, correctAnswer, pointsEarned, totalScore } = resultData;
  
  // Hiển thị kết quả
  const resultElement = document.getElementById('answer-result');
  resultElement.textContent = isCorrect ? 'Đúng!' : 'Sai!';
  resultElement.className = isCorrect ? 'correct' : 'incorrect';
  
  // Hiển thị đáp án đúng
  document.getElementById('correct-answer').textContent = `Đáp án đúng: ${correctAnswer}`;
  
  // Hiển thị điểm
  document.getElementById('points-earned').textContent = `+${pointsEarned} điểm`;
  document.getElementById('total-score').textContent = `Tổng điểm: ${totalScore}`;
  
  // Hiển thị màn hình kết quả
  showScreen('result-screen');
  
  // Tự động chuyển sang màn hình chờ sau 3 giây
  setTimeout(() => {
    showScreen('waiting-for-next-question-screen');
  }, 3000);
}
```

### 4.4. Hiển Thị Bảng Xếp Hạng

```javascript
function updateLeaderboard(players) {
  // Sắp xếp người chơi theo điểm số
  const sortedPlayers = [...players].sort((a, b) => b.score - a.score);
  
  // Cập nhật UI
  const leaderboardElement = document.getElementById('leaderboard');
  leaderboardElement.innerHTML = '';
  
  sortedPlayers.forEach((player, index) => {
    const playerElement = document.createElement('div');
    playerElement.classList.add('player-row');
    
    // Thêm class cho người chơi hiện tại
    const currentUser = JSON.parse(localStorage.getItem('currentUser'));
    if (currentUser && player.userId === currentUser.id) {
      playerElement.classList.add('current-player');
    }
    
    // Thêm class cho top 3
    if (index < 3) {
      playerElement.classList.add(`rank-${index + 1}`);
    }
    
    playerElement.innerHTML = `
      <span class="rank">${index + 1}</span>
      <span class="username">${player.username}</span>
      <span class="score">${player.score}</span>
      <span class="correct-answers">${player.correctAnswers}/${player.totalAnswers}</span>
    `;
    
    leaderboardElement.appendChild(playerElement);
  });
}
```

### 4.5. Hiển Thị Kết Quả Cuối Cùng

```javascript
function showGameResults(gameEndData) {
  const { leaderboard, gameStats } = gameEndData;
  
  // Hiển thị top 3
  const top3Container = document.getElementById('top-3-container');
  top3Container.innerHTML = '';
  
  // Lấy top 3 người chơi
  const top3 = leaderboard.slice(0, 3);
  
  // Hiển thị podium
  top3.forEach((player, index) => {
    const podiumPosition = document.createElement('div');
    podiumPosition.classList.add('podium-position', `position-${index + 1}`);
    
    podiumPosition.innerHTML = `
      <div class="player-name">${player.username}</div>
      <div class="player-score">${player.score}</div>
      <div class="podium-block rank-${index + 1}">
        <span class="rank-number">${index + 1}</span>
      </div>
    `;
    
    top3Container.appendChild(podiumPosition);
  });
  
  // Hiển thị bảng xếp hạng đầy đủ
  updateLeaderboard(leaderboard);
  
  // Hiển thị thống kê game
  document.getElementById('total-players').textContent = gameStats.totalPlayers;
  document.getElementById('average-score').textContent = gameStats.averageScore;
  document.getElementById('highest-score').textContent = gameStats.highestScore;
  document.getElementById('game-duration').textContent = gameStats.duration;
  
  // Hiển thị màn hình kết quả
  showScreen('game-results-screen');
  
  // Hiển thị nút "Chơi lại" cho host
  const playAgainButton = document.getElementById('play-again-button');
  const currentUser = JSON.parse(localStorage.getItem('currentUser'));
  const currentRoom = JSON.parse(localStorage.getItem('currentRoom'));
  
  if (currentUser && currentRoom && currentUser.id === currentRoom.hostId) {
    playAgainButton.style.display = 'block';
  } else {
    playAgainButton.style.display = 'none';
  }
}
```

## 5. Xử Lý Sự Kiện Phòng

```javascript
function setupRoomEventHandlers() {
  // Sự kiện khi tham gia phòng thành công
  socketManager.on('room-joined', (data) => {
    console.log('Đã tham gia phòng:', data);
    
    // Hiển thị thông báo chào mừng
    showToast(data.message);
    
    // Cập nhật UI dựa trên vai trò (host/player)
    updateRoleUI(data.isHost);
  });
  
  // Sự kiện khi có người chơi mới tham gia
  socketManager.on('player-joined', (data) => {
    console.log('Người chơi mới tham gia:', data);
    
    // Hiển thị thông báo
    showToast(`${data.username} đã tham gia phòng`);
  });
  
  // Sự kiện khi có người chơi rời phòng
  socketManager.on('player-left', (data) => {
    console.log('Người chơi rời phòng:', data);
    
    // Hiển thị thông báo
    showToast(`${data.username} đã rời phòng`);
  });
  
  // Sự kiện cập nhật danh sách người chơi
  socketManager.on('room-players-updated', (data) => {
    console.log('Cập nhật danh sách người chơi:', data);
    
    // Cập nhật UI phòng chờ
    renderWaitingRoom(data);
  });
  
  // Sự kiện thay đổi host
  socketManager.on('host-changed', (data) => {
    console.log('Host mới:', data);
    
    // Hiển thị thông báo
    showToast(data.message);
    
    // Cập nhật UI nếu người dùng hiện tại là host mới
    const currentUser = JSON.parse(localStorage.getItem('currentUser'));
    if (currentUser && currentUser.id === data.newHostId) {
      updateRoleUI(true);
    }
  });
}
```

## 6. Xử Lý Lỗi và Kết Nối Lại

```javascript
function setupErrorHandling() {
  // Xử lý lỗi API
  window.addEventListener('unhandledrejection', (event) => {
    if (event.reason && event.reason.message) {
      showErrorToast(event.reason.message);
    } else {
      showErrorToast('Đã xảy ra lỗi không xác định');
    }
  });
  
  // Xử lý mất kết nối
  window.addEventListener('offline', () => {
    showErrorToast('Mất kết nối internet. Đang thử kết nối lại...');
  });
  
  window.addEventListener('online', () => {
    showToast('Đã khôi phục kết nối internet');
    
    // Thử kết nối lại WebSocket
    if (!socketManager.isConnected) {
      socketManager.connect().then(() => {
        // Kết nối lại thành công, thử tham gia lại phòng
        rejoinRoom();
      }).catch(() => {
        showErrorToast('Không thể kết nối lại với máy chủ');
      });
    }
  });
}

// Hàm tham gia lại phòng sau khi mất kết nối
async function rejoinRoom() {
  const currentRoom = JSON.parse(localStorage.getItem('currentRoom'));
  const currentUser = JSON.parse(localStorage.getItem('currentUser'));
  
  if (currentRoom && currentUser) {
    try {
      await joinRoom({
        roomCode: currentRoom.roomCode,
        userId: currentUser.id,
        username: currentUser.username
      });
      
      showToast('Đã tham gia lại phòng thành công');
    } catch (error) {
      showErrorToast('Không thể tham gia lại phòng: ' + error.message);
    }
  }
}
```

## 7. Khởi Tạo Ứng Dụng

```javascript
// Khởi tạo ứng dụng
async function initializeApp() {
  try {
    // Thiết lập xử lý sự kiện
    setupRoomEventHandlers();
    setupGameEventHandlers();
    setupErrorHandling();
    
    // Kiểm tra đăng nhập
    const token = localStorage.getItem('token');
    if (!token) {
      // Chuyển đến trang đăng nhập nếu chưa đăng nhập
      navigateToLogin();
      return;
    }
    
    // Lấy thông tin người dùng
    const currentUser = JSON.parse(localStorage.getItem('currentUser'));
    if (!currentUser) {
      try {
        const userInfo = await callApi('/user/profile', 'GET');
        localStorage.setItem('currentUser', JSON.stringify(userInfo.data));
      } catch (error) {
        // Token không hợp lệ hoặc hết hạn
        localStorage.removeItem('token');
        navigateToLogin();
        return;
      }
    }
    
    // Kiểm tra xem người dùng đang trong phòng nào
    const currentRoom = JSON.parse(localStorage.getItem('currentRoom'));
    if (currentRoom) {
      // Kết nối WebSocket
      await socketManager.connect();
      
      // Thử tham gia lại phòng
      await rejoinRoom();
    }
    
    // Hiển thị màn hình chính
    showScreen('main-screen');
  } catch (error) {
    console.error('Lỗi khởi tạo ứng dụng:', error);
    showErrorToast('Không thể khởi tạo ứng dụng: ' + error.message);
  }
}

// Khởi chạy ứng dụng khi trang đã tải xong
document.addEventListener('DOMContentLoaded', initializeApp);
```

## 8. Tối Ưu Hiệu Suất

### 8.1. Lazy Loading

```javascript
// Lazy loading các thành phần không cần thiết ngay lập tức
function setupLazyLoading() {
  // Lazy load hình ảnh
  const lazyImages = document.querySelectorAll('.lazy-image');
  
  const imageObserver = new IntersectionObserver((entries, observer) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const img = entry.target;
        img.src = img.dataset.src;
        img.classList.remove('lazy-image');
        observer.unobserve(img);
      }
    });
  });
  
  lazyImages.forEach(img => {
    imageObserver.observe(img);
  });
  
  // Lazy load các module JavaScript
  if ('requestIdleCallback' in window) {
    requestIdleCallback(() => {
      // Tải các module không cần thiết ngay lập tức
      import('./analytics.js').then(module => {
        module.initialize();
      });
    });
  } else {
    setTimeout(() => {
      import('./analytics.js').then(module => {
        module.initialize();
      });
    }, 5000);
  }
}
```

### 8.2. Debounce và Throttle

```javascript
// Debounce function để tránh gọi hàm quá nhiều lần
function debounce(func, wait) {
  let timeout;
  
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

// Throttle function để giới hạn tần suất gọi hàm
function throttle(func, limit) {
  let inThrottle;
  
  return function executedFunction(...args) {
    if (!inThrottle) {
      func(...args);
      inThrottle = true;
      
      setTimeout(() => {
        inThrottle = false;
      }, limit);
    }
  };
}

// Sử dụng debounce cho việc cập nhật UI
const debouncedUpdateLeaderboard = debounce(updateLeaderboard, 300);

// Sử dụng throttle cho việc gửi cập nhật vị trí
const throttledSendPosition = throttle((x, y) => {
  socketManager.send('update-position', { x, y });
}, 100);
```

## 9. Kiểm Thử

### 9.1. Kiểm Thử Kết Nối

```javascript
// Kiểm tra kết nối API và WebSocket
async function testConnections() {
  console.log('Đang kiểm tra kết nối...');
  
  try {
    // Kiểm tra API
    const apiResult = await callApi('/health', 'GET');
    console.log('Kết nối API: OK', apiResult);
    
    // Kiểm tra WebSocket
    await socketManager.connect();
    console.log('Kết nối WebSocket: OK');
    
    return true;
  } catch (error) {
    console.error('Lỗi kết nối:', error);
    return false;
  }
}
```

### 9.2. Kiểm Thử Tự Động

```javascript
// Kiểm thử tự động các chức năng
async function runAutomatedTests() {
  console.log('Bắt đầu kiểm thử tự động...');
  
  // Kiểm tra kết nối
  if (!await testConnections()) {
    console.error('Kiểm thử thất bại: Không thể kết nối');
    return;
  }
  
  // Kiểm thử tạo phòng
  try {
    const roomData = await createRoom({
      name: 'Test Room',
      maxPlayers: 10,
      isPublic: true,
      hostId: 123
    });
    
    console.log('Tạo phòng thành công:', roomData);
    
    // Kiểm thử tham gia phòng
    const joinResult = await joinRoom({
      roomCode: roomData.roomCode,
      userId: 456,
      username: 'TestPlayer'
    });
    
    console.log('Tham gia phòng thành công:', joinResult);
    
    // Kiểm thử bắt đầu game
    const startResult = await startGame(roomData.roomCode, 123);
    console.log('Bắt đầu game:', startResult);
    
    // Kiểm thử rời phòng
    const leaveResult = await leaveRoom(roomData.roomCode, 456);
    console.log('Rời phòng:', leaveResult);
    
    console.log('Tất cả kiểm thử đã hoàn thành thành công!');
  } catch (error) {
    console.error('Kiểm thử thất bại:', error);
  }
}
```

## 10. Kết Luận

Hướng dẫn này cung cấp các thành phần cần thiết để tích hợp frontend với API và WebSocket của hệ thống game Quizizz. Bằng cách tuân thủ các định dạng và quy trình được mô tả, bạn có thể xây dựng một giao diện người dùng mượt mà và đáp ứng thời gian thực.

Các thành phần chính:
- **API Client**: Giao tiếp với backend thông qua RESTful API
- **WebSocket Manager**: Xử lý kết nối và sự kiện thời gian thực
- **UI Components**: Hiển thị phòng chờ, câu hỏi, kết quả và bảng xếp hạng
- **Error Handling**: Xử lý lỗi và kết nối lại

Để đảm bảo trải nghiệm người dùng tốt nhất, hãy chú ý đến việc xử lý lỗi, tối ưu hiệu suất và kiểm thử kỹ lưỡng trước khi triển khai.