namespace ConsoleApp1.Config
{
    public static class RoomManagementConstants
    {
        // Room state constants
        public const string ROOM_STATE_WAITING = "waiting";
        public const string ROOM_STATE_READY = "ready";
        public const string ROOM_STATE_PLAYING = "playing";
        public const string ROOM_STATE_FINISHED = "finished";
        
        // Room capacity
        public const int MAX_PLAYERS_PER_ROOM = 10;
        public const int MIN_PLAYERS_TO_START = 2;
        
        // Timing constants (in seconds)
        public const int ROOM_TIMEOUT_SECONDS = 1800; // 30 minutes
        public const int PLAYER_READY_TIMEOUT_SECONDS = 60;
        public const int GAME_START_DELAY_SECONDS = 5;
        
        // Error messages
        public const string ERROR_ROOM_NOT_FOUND = "Room not found";
        public const string ERROR_ROOM_FULL = "Room is full";
        public const string ERROR_INSUFFICIENT_PLAYERS = "Not enough players to start game";
        public const string ERROR_GAME_ALREADY_STARTED = "Game has already started";
        public const string ERROR_PLAYER_NOT_IN_ROOM = "Player is not in this room";
        
        // Events - Nested class để tổ chức tốt hơn
        public static class Events
        {
            public const string RoomJoined = "room-joined";
            public const string PlayerJoined = "player-joined";
            public const string PlayerLeft = "player-left";
            public const string RoomPlayersUpdated = "room-players-updated";
            public const string HostChanged = "host-changed";
            public const string GameStarted = "game-started";
            public const string PlayersUpdate = "players-update";
        }
        
        // Limits for validation
        public static class Limits
        {
            public const int MinRoomCodeLength = 4;
            public const int MaxRoomCodeLength = 10;
            public const int MinUserNameLength = 2;
            public const int MaxUserNameLength = 50;
        }
    }
}
