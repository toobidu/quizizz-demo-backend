-- ===============================
-- 1. Bảng users
-- ===============================
CREATE TABLE users (
                       id SERIAL PRIMARY KEY,
                       username TEXT NOT NULL UNIQUE,
                       password TEXT NOT NULL,
                       type_account TEXT NOT NULL
);

-- ===============================
-- 2. Bảng roles
-- ===============================
CREATE TABLE roles (
                       id SERIAL PRIMARY KEY,
                       role_name TEXT NOT NULL UNIQUE
);

-- ===============================
-- 3. Bảng permissions
-- ===============================
CREATE TABLE permissions (
                             id SERIAL PRIMARY KEY,
                             permission_name TEXT NOT NULL UNIQUE
);

-- ===============================
-- 4. Bảng user_roles (many-to-many)
-- ===============================
CREATE TABLE user_roles (
                            user_id INT NOT NULL,
                            role_id INT NOT NULL,
                            PRIMARY KEY (user_id, role_id),
                            FOREIGN KEY (user_id) REFERENCES users(id),
                            FOREIGN KEY (role_id) REFERENCES roles(id)
);

-- ===============================
-- 5. Bảng role_permissions (many-to-many)
-- ===============================
CREATE TABLE role_permissions (
                                  role_id INT NOT NULL,
                                  permission_id INT NOT NULL,
                                  PRIMARY KEY (role_id, permission_id),
                                  FOREIGN KEY (role_id) REFERENCES roles(id),
                                  FOREIGN KEY (permission_id) REFERENCES permissions(id)
);

-- ===============================
-- 6. Bảng rooms
-- ===============================
CREATE TABLE rooms (
                       id SERIAL PRIMARY KEY,
                       room_code TEXT NOT NULL UNIQUE,
                       room_name TEXT NOT NULL,
                       is_private BOOLEAN NOT NULL,
                       owner_id INT NOT NULL,
                       FOREIGN KEY (owner_id) REFERENCES users(id)
);

-- ===============================
-- 7. Bảng room_players
-- ===============================
CREATE TABLE room_players (
                              room_id INT NOT NULL,
                              user_id INT NOT NULL,
                              score INT NOT NULL,
                              time_taken INTERVAL NOT NULL,
                              PRIMARY KEY (room_id, user_id),
                              FOREIGN KEY (room_id) REFERENCES rooms(id),
                              FOREIGN KEY (user_id) REFERENCES users(id)
);

-- ===============================
-- 8. Bảng questions
-- ===============================
CREATE TABLE questions (
                           id SERIAL PRIMARY KEY,
                           question_text TEXT NOT NULL
);

-- ===============================
-- 9. Bảng answers
-- ===============================
CREATE TABLE answers (
                         id SERIAL PRIMARY KEY,
                         question_id INT NOT NULL,
                         answer_text TEXT NOT NULL,
                         is_correct BOOLEAN NOT NULL,
                         FOREIGN KEY (question_id) REFERENCES questions(id)
);

-- ===============================
-- 10. Bảng user_answers
-- ===============================
CREATE TABLE user_answers (
                              user_id INT NOT NULL,
                              room_id INT NOT NULL,
                              question_id INT NOT NULL,
                              answer_id INT NOT NULL,
                              is_correct BOOLEAN NOT NULL,
                              time_taken INTERVAL NOT NULL,
                              PRIMARY KEY (user_id, room_id, question_id),
                              FOREIGN KEY (user_id) REFERENCES users(id),
                              FOREIGN KEY (room_id) REFERENCES rooms(id),
                              FOREIGN KEY (question_id) REFERENCES questions(id),
                              FOREIGN KEY (answer_id) REFERENCES answers(id)
);

-- ===============================
-- 11. Bảng ranks
-- ===============================
CREATE TABLE ranks (
                       id SERIAL PRIMARY KEY,
                       user_id INT NOT NULL,
                       total_score INT NOT NULL,
                       games_played INT NOT NULL,
                       updated_at TIMESTAMP NOT NULL,
                       FOREIGN KEY (user_id) REFERENCES users(id)
);
