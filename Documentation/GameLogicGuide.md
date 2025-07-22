# Hướng dẫn Logic Chơi Game, Router, API và Socket

## 1. Tổng quan về Kiến trúc

Hệ thống game Quizizz được xây dựng với kiến trúc phân lớp rõ ràng:

- **Controller**: Xử lý các request HTTP từ client
- **Router**: Định tuyến các request đến controller phù hợp
- **Service**: Chứa logic nghiệp vụ
- **Repository**: Tương tác với cơ sở dữ liệu
- **Socket Service**: Xử lý giao tiếp thời gian thực qua WebSocket

## 2. Luồng Chơi Game

### 2.1. Khởi tạo Game

1. **Tạo phòng**:
   - Host tạo phòng qua API `/api/room/create`
   - Hệ thống tạo mã phòng ngẫu nhiên và lưu thông tin phòng

2. **Tham gia phòng**:
   - Người chơi tham gia qua API `/api/room/join` với mã phòng
   - Khi tham gia thành công, người chơi được kết nối WebSocket

3. **Bắt đầu game**:
   - Host bắt đầu game qua API `/api/game/start`
   - Hệ thống khởi tạo GameSession và gửi thông báo qua WebSocket

### 2.2. Quá trình Chơi Game

1. **Đếm ngược**:
   - Gửi sự kiện `countdown` (3, 2, 1) trước khi bắt đầu

2. **Gửi câu hỏi**:
   - Hệ thống gửi câu hỏi qua sự kiện `question`
   - Mỗi câu hỏi có thời gian giới hạn

3. **Nhận câu trả lời**:
   - Người chơi gửi câu trả lời qua sự kiện `submit-answer`
   - Hệ thống kiểm tra tính đúng đắn và tính điểm

4. **Cập nhật tiến độ**:
   - Gửi sự kiện `player-progress` để cập nhật bảng xếp hạng

5. **Kết thúc game**:
   - Sau khi trả lời hết câu hỏi, gửi sự kiện `game-ended`
   - Hiển thị kết quả cuối cùng

## 3. API Endpoints

### 3.1. Quản lý Phòng

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/room/create` | POST | Tạo phòng mới |
| `/api/room/join` | POST | Tham gia phòng |
| `/api/room/leave` | POST | Rời khỏi phòng |

### 3.2. Quản lý Game

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/api/game/start` | POST | Bắt đầu game |
| `/api/game/question` | POST | Gửi câu hỏi |

## 4. Format Request/Response API

### 4.1. Tạo Phòng

**Request:**
```json
{
  "name": "Phòng Quiz Vui",
  "maxPlayers": 10,
  "isPublic": true,
  "hostId": 123
}
```

**Response:**
```json
{
  "success": true,
  "message": "Phòng đã được tạo",
  "data": {
    "roomId": 456,
    "roomCode": "ABCDEF",
    "name": "Phòng Quiz Vui",
    "maxPlayers": 10,
    "isPublic": true,
    "hostId": 123,
    "createdAt": "2023-06-15T10:30:00Z"
  },
  "statusCode": 200,
  "path": "/api/room/create"
}
```

### 4.2. Tham Gia Phòng

**Request:**
```json
{
  "roomCode": "ABCDEF",
  "userId": 789,
  "username": "player123"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Đã tham gia phòng",
  "data": {
    "roomId": 456,
    "roomCode": "ABCDEF",
    "name": "Phòng Quiz Vui",
    "players": [
      {
        "userId": 123,
        "username": "host456",
        "isHost": true,
        "score": 0
      },
      {
        "userId": 789,
        "username": "player123",
        "isHost": false,
        "score": 0
      }
    ],
    "totalPlayers": 2,
    "maxPlayers": 10
  },
  "statusCode": 200,
  "path": "/api/room/join"
}
```

### 4.3. Bắt Đầu Game

**Request:**
```json
{
  "roomCode": "ABCDEF",
  "hostUserId": 123
}
```

**Response:**
```json
{
  "success": true,
  "message": "Game đã bắt đầu",
  "data": "Game started successfully",
  "statusCode": 200,
  "path": "/api/game/start"
}
```

## 5. Sự Kiện Socket

### 5.1. Quản lý Phòng

| Sự kiện | Hướng | Mô tả |
|---------|-------|-------|
| `room-joined` | Server → Client | Gửi khi người chơi tham gia phòng thành công |
| `player-joined` | Server → Client | Thông báo có người chơi mới tham gia |
| `player-left` | Server → Client | Thông báo có người chơi rời phòng |
| `room-players-updated` | Server → Client | Cập nhật danh sách người chơi trong phòng |
| `host-changed` | Server → Client | Thông báo thay đổi host |

### 5.2. Luồng Game

| Sự kiện | Hướng | Mô tả |
|---------|-------|-------|
| `game-started` | Server → Client | Thông báo game bắt đầu |
| `countdown` | Server → Client | Đếm ngược trước khi bắt đầu |
| `question` | Server → Client | Gửi câu hỏi đến người chơi |
| `submit-answer` | Client → Server | Người chơi gửi câu trả lời |
| `answer-result` | Server → Client | Kết quả câu trả lời |
| `player-progress` | Server → Client | Cập nhật tiến độ người chơi |
| `game-timer-update` | Server → Client | Cập nhật thời gian còn lại |
| `game-ended` | Server → Client | Thông báo game kết thúc |

## 6. Format Dữ Liệu Socket

### 6.1. Tham Gia Phòng

```json
{
  "type": "room-joined",
  "data": {
    "roomCode": "ABCDEF",
    "isHost": false,
    "message": "Bạn đã tham gia phòng thành công"
  },
  "timestamp": "2023-06-15T10:35:00Z"
}
```

### 6.2. Người Chơi Mới

```json
{
  "type": "player-joined",
  "data": {
    "userId": 789,
    "username": "player123",
    "score": 0,
    "timeTaken": "00:00:00"
  },
  "timestamp": "2023-06-15T10:35:00Z"
}
```

### 6.3. Cập Nhật Danh Sách Người Chơi

```json
{
  "type": "room-players-updated",
  "data": {
    "roomCode": "ABCDEF",
    "players": [
      {
        "userId": 123,
        "username": "host456",
        "score": 0,
        "isHost": true,
        "status": "ready",
        "timeTaken": "00:00:00"
      },
      {
        "userId": 789,
        "username": "player123",
        "score": 0,
        "isHost": false,
        "status": "waiting",
        "timeTaken": "00:00:00"
      }
    ],
    "totalPlayers": 2,
    "maxPlayers": 10,
    "status": "waiting",
    "host": "host456"
  },
  "timestamp": "2023-06-15T10:35:05Z"
}
```

### 6.4. Gửi Câu Hỏi

```json
{
  "type": "question",
  "data": {
    "questionId": 42,
    "questionIndex": 0,
    "totalQuestions": 10,
    "questionText": "Thủ đô của Việt Nam là gì?",
    "options": [
      "Hà Nội",
      "Hồ Chí Minh",
      "Đà Nẵng",
      "Hải Phòng"
    ],
    "timeLimit": 30,
    "questionType": "multiple-choice",
    "media": null
  },
  "timestamp": "2023-06-15T10:40:00Z"
}
```

### 6.5. Gửi Câu Trả Lời

```json
{
  "type": "submit-answer",
  "data": {
    "questionIndex": 0,
    "selectedAnswer": "Hà Nội",
    "submitTime": 1686825630000
  }
}
```

### 6.6. Kết Quả Câu Trả Lời

```json
{
  "type": "answer-result",
  "data": {
    "questionIndex": 0,
    "isCorrect": true,
    "correctAnswer": "Hà Nội",
    "pointsEarned": 100,
    "totalScore": 100,
    "timeToAnswer": 5
  },
  "timestamp": "2023-06-15T10:40:35Z"
}
```

### 6.7. Cập Nhật Tiến Độ Người Chơi

```json
{
  "type": "player-progress",
  "data": {
    "players": [
      {
        "userId": 123,
        "username": "host456",
        "score": 100,
        "correctAnswers": 1,
        "totalAnswers": 1,
        "rank": 1
      },
      {
        "userId": 789,
        "username": "player123",
        "score": 80,
        "correctAnswers": 1,
        "totalAnswers": 1,
        "rank": 2
      }
    ]
  },
  "timestamp": "2023-06-15T10:40:40Z"
}
```

### 6.8. Kết Thúc Game

```json
{
  "type": "game-ended",
  "data": {
    "leaderboard": [
      {
        "userId": 123,
        "username": "host456",
        "score": 950,
        "correctAnswers": 10,
        "totalAnswers": 10,
        "rank": 1
      },
      {
        "userId": 789,
        "username": "player123",
        "score": 820,
        "correctAnswers": 9,
        "totalAnswers": 10,
        "rank": 2
      }
    ],
    "gameStats": {
      "totalPlayers": 2,
      "averageScore": 885,
      "highestScore": 950,
      "duration": "00:05:30"
    }
  },
  "timestamp": "2023-06-15T10:45:00Z"
}
```

## 7. Hướng Dẫn Cho Frontend

### 7.1. Kết Nối WebSocket

```javascript
// Kết nối WebSocket
const socket = new WebSocket('ws://your-server-url/ws');

// Xử lý khi kết nối mở
socket.onopen = () => {
  console.log('Kết nối WebSocket đã được thiết lập');
};

// Xử lý khi nhận tin nhắn
socket.onmessage = (event) => {
  const message = JSON.parse(event.data);
  handleSocketMessage(message);
};

// Xử lý khi kết nối đóng
socket.onclose = () => {
  console.log('Kết nối WebSocket đã đóng');
};

// Xử lý lỗi
socket.onerror = (error) => {
  console.error('Lỗi WebSocket:', error);
};
```

### 7.2. Xử Lý Tin Nhắn Socket

```javascript
function handleSocketMessage(message) {
  const { type, data } = message;
  
  switch (type) {
    case 'room-joined':
      handleRoomJoined(data);
      break;
    case 'player-joined':
      handlePlayerJoined(data);
      break;
    case 'player-left':
      handlePlayerLeft(data);
      break;
    case 'room-players-updated':
      handleRoomPlayersUpdated(data);
      break;
    case 'host-changed':
      handleHostChanged(data);
      break;
    case 'game-started':
      handleGameStarted(data);
      break;
    case 'countdown':
      handleCountdown(data);
      break;
    case 'question':
      handleQuestion(data);
      break;
    case 'answer-result':
      handleAnswerResult(data);
      break;
    case 'player-progress':
      handlePlayerProgress(data);
      break;
    case 'game-timer-update':
      handleGameTimerUpdate(data);
      break;
    case 'game-ended':
      handleGameEnded(data);
      break;
    default:
      console.log('Không xử lý được tin nhắn:', type);
  }
}
```

### 7.3. Gửi Câu Trả Lời

```javascript
function submitAnswer(questionIndex, selectedAnswer) {
  const data = {
    type: 'submit-answer',
    data: {
      questionIndex,
      selectedAnswer,
      submitTime: Date.now()
    }
  };
  
  socket.send(JSON.stringify(data));
}
```

### 7.4. Hiển Thị Bảng Xếp Hạng

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
    playerElement.innerHTML = `
      <span class="rank">${index + 1}</span>
      <span class="username">${player.username}</span>
      <span class="score">${player.score}</span>
    `;
    leaderboardElement.appendChild(playerElement);
  });
}
```

## 8. Xử Lý Lỗi và Tình Huống Đặc Biệt

### 8.1. Người Chơi Mất Kết Nối

- Hệ thống giữ thông tin người chơi trong phòng trong một khoảng thời gian
- Nếu người chơi kết nối lại, họ sẽ được đưa trở lại phòng
- Nếu không kết nối lại sau thời gian chờ, họ sẽ bị xóa khỏi phòng

### 8.2. Host Rời Phòng

- Hệ thống tự động chuyển quyền host cho người chơi tiếp theo
- Gửi sự kiện `host-changed` để thông báo cho tất cả người chơi

### 8.3. Phòng Trống

- Nếu không còn người chơi nào trong phòng, hệ thống sẽ xóa phòng
- Giải phóng tài nguyên và mã phòng

## 9. Tối Ưu Hiệu Suất

### 9.1. Giảm Thiểu Dữ Liệu Truyền Tải

- Chỉ gửi dữ liệu cần thiết qua WebSocket
- Sử dụng định dạng JSON nhỏ gọn

### 9.2. Tránh Gửi Trùng Lặp

- Kiểm tra và ngăn chặn gửi sự kiện trùng lặp trong thời gian ngắn
- Sử dụng cơ chế throttling cho các sự kiện thường xuyên như cập nhật thời gian

### 9.3. Xử Lý Đồng Thời

- Sử dụng ConcurrentDictionary để quản lý phòng và kết nối
- Đảm bảo thread-safe khi nhiều người chơi tương tác cùng lúc

## 10. Kiểm Thử

### 10.1. Kiểm Thử API

```javascript
// Kiểm thử API tạo phòng
async function testCreateRoom() {
  const response = await fetch('/api/room/create', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      name: 'Test Room',
      maxPlayers: 10,
      isPublic: true,
      hostId: 123
    })
  });
  
  const result = await response.json();
  console.log('Create Room Result:', result);
  return result;
}

// Kiểm thử API tham gia phòng
async function testJoinRoom(roomCode) {
  const response = await fetch('/api/room/join', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      roomCode,
      userId: 789,
      username: 'testPlayer'
    })
  });
  
  const result = await response.json();
  console.log('Join Room Result:', result);
  return result;
}
```

### 10.2. Kiểm Thử Socket

```javascript
// Mô phỏng nhiều người chơi tham gia
function simulateMultiplePlayers(roomCode, count) {
  const players = [];
  
  for (let i = 0; i < count; i++) {
    const socket = new WebSocket('ws://your-server-url/ws');
    
    socket.onopen = () => {
      // Gửi yêu cầu tham gia phòng
      socket.send(JSON.stringify({
        type: 'join-room',
        data: {
          roomCode,
          userId: 1000 + i,
          username: `player${i}`
        }
      }));
    };
    
    socket.onmessage = (event) => {
      console.log(`Player ${i} received:`, JSON.parse(event.data));
    };
    
    players.push(socket);
  }
  
  return players;
}
```

## 11. Kết Luận

Hệ thống game Quizizz được thiết kế với kiến trúc module hóa, cho phép dễ dàng mở rộng và bảo trì. Giao tiếp giữa client và server được thực hiện thông qua API RESTful và WebSocket, đảm bảo trải nghiệm người dùng mượt mà và thời gian thực.

Các thành phần chính:
- **GameFlowOrchestrator**: Điều phối luồng game
- **RoomManagementService**: Quản lý phòng và người chơi
- **PlayerInteractionService**: Xử lý tương tác của người chơi
- **ScoringService**: Tính điểm và xếp hạng

Việc tuân thủ các định dạng API và sự kiện Socket như đã mô tả sẽ đảm bảo tích hợp suôn sẻ giữa frontend và backend.