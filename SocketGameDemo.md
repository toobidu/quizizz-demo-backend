# Socket Game Logic Demo

## Luồng hoạt động mới

### 1. Host khởi tạo game
```javascript
// Host gửi danh sách câu hỏi và thời gian tổng
socket.emit('start-game', roomCode, questions, gameTimeLimit);

// questions format:
[
  {
    "questionId": 1,
    "question": "Câu hỏi 1?",
    "options": ["A", "B", "C", "D"],
    "correctAnswer": "A",
    "type": "multiple_choice",
    "topic": "Math"
  },
  // ... more questions
]
```

### 2. Game bắt đầu
- Server khởi tạo timer toàn cục (gameTimeLimit)
- Tất cả người chơi nhận event `game-started`
- Mỗi người chơi request câu hỏi đầu tiên

### 3. Người chơi lấy câu hỏi
```javascript
// Player request câu hỏi tiếp theo
socket.emit('get-next-question', roomCode, username);

// Server trả về câu hỏi (chỉ gửi cho player đó)
socket.on('next-question', (data) => {
  // data.question, data.questionIndex, data.totalQuestions, data.timeRemaining
});

// Nếu đã hoàn thành tất cả câu hỏi
socket.on('all-questions-completed', (data) => {
  // Hiển thị thông báo chờ
});
```

### 4. Trả lời câu hỏi
```javascript
// Player gửi câu trả lời
socket.emit('submit-answer', roomCode, username, answer);

// Sau khi trả lời, tự động request câu hỏi tiếp theo
socket.emit('get-next-question', roomCode, username);
```

### 5. Theo dõi tiến độ
```javascript
// Server broadcast tiến độ tất cả người chơi
socket.on('players-progress-update', (data) => {
  // data.playersProgress, data.gameTimeRemaining
});

// Cập nhật khi có người trả lời
socket.on('answer-received', (data) => {
  // data.username, data.questionIndex, data.totalAnswered, data.totalQuestions
});
```

### 6. Kết thúc game
```javascript
// Khi hết thời gian, server tự động kết thúc
socket.on('game-ended', (data) => {
  // data.finalResults, data.message
  // Hiển thị bảng xếp hạng cuối cùng
});
```

## Đặc điểm chính

1. **Timer toàn cục**: Một timer duy nhất cho cả game, không phải từng câu hỏi
2. **Trả lời liên tục**: Người chơi có thể trả lời liên tục mà không cần chờ
3. **Chờ đồng bộ**: Người hoàn thành sớm phải chờ timer kết thúc
4. **Hỗ trợ nhiều room**: Mỗi room có session riêng biệt
5. **Theo dõi tiến độ**: Real-time tracking cho tất cả người chơi

## Events mới

### Client → Server
- `start-game(roomCode, questions, gameTimeLimit)`
- `get-next-question(roomCode, username)`
- `submit-answer(roomCode, username, answer)`

### Server → Client
- `game-started(totalQuestions, gameTimeLimit, startTime)`
- `next-question(question, questionIndex, totalQuestions, timeRemaining)`
- `all-questions-completed(message, completionTime)`
- `players-progress-update(playersProgress, gameTimeRemaining)`
- `answer-received(username, questionIndex, totalAnswered, totalQuestions)`
- `game-ended(finalResults, message)`

## Cấu trúc dữ liệu

### RoomGameSession
```csharp
{
  TotalQuestions: int,
  GameTimeLimit: int, // seconds
  GameStartTime: DateTime,
  IsGameActive: bool,
  IsGameEnded: bool,
  PlayerResults: Dictionary<string, PlayerGameResult>,
  Questions: List<QuestionData>,
  GameTimer: Timer
}
```

### PlayerGameResult
```csharp
{
  Username: string,
  Answers: List<PlayerAnswer>,
  CompletionTime: DateTime?,
  Score: int,
  HasFinishedAllQuestions: bool,
  TotalQuestions: int
}
```