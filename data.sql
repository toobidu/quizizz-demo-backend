-- ===============================
-- 1. Hàm cập nhật created_at và updated_at
-- ===============================
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        NEW.created_at = CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh';
        NEW.updated_at = CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh';
    ELSIF TG_OP = 'UPDATE' THEN
        NEW.updated_at = CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh';
        NEW.created_at = OLD.created_at;
END IF;
RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ===============================
-- 3. Bảng users
-- ===============================
CREATE TABLE users
(
    id           SERIAL PRIMARY KEY,
    username     TEXT      NOT NULL UNIQUE,
    full_name    TEXT      NOT NULL,
    email        TEXT      NOT NULL UNIQUE,
    phone_number TEXT      NOT NULL UNIQUE,
    address      TEXT      NOT NULL,
    password     TEXT      NOT NULL,
    type_account TEXT      NOT NULL,
    created_at   TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at   TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh')
);

CREATE TRIGGER update_users_timestamp
    BEFORE INSERT OR
UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 4. Bảng roles
-- ===============================
CREATE TABLE roles
(
    id         SERIAL PRIMARY KEY,
    role_name  TEXT      NOT NULL UNIQUE,
    description TEXT     NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh')
);

CREATE TRIGGER update_roles_timestamp
    BEFORE INSERT OR
UPDATE ON roles
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 5. Bảng permissions
-- ===============================
CREATE TABLE permissions
(
    id              SERIAL PRIMARY KEY,
    permission_name TEXT      NOT NULL UNIQUE,
    description     TEXT      NOT NULL,
    created_at      TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at      TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh')
);

CREATE TRIGGER update_permissions_timestamp
    BEFORE INSERT OR
UPDATE ON permissions
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 6. Bảng user_roles (many-to-many)
-- ===============================
CREATE TABLE user_roles
(
    user_id    INT       NOT NULL,
    role_id    INT       NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE
);

CREATE TRIGGER update_user_roles_timestamp
    BEFORE INSERT OR
UPDATE ON user_roles
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 7. Bảng role_permissions (many-to-many)
-- ===============================
CREATE TABLE role_permissions
(
    role_id       INT       NOT NULL,
    permission_id INT       NOT NULL,
    created_at    TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at    TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    PRIMARY KEY (role_id, permission_id),
    FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE,
    FOREIGN KEY (permission_id) REFERENCES permissions (id) ON DELETE CASCADE
);

CREATE TRIGGER update_role_permissions_timestamp
    BEFORE INSERT OR
UPDATE ON role_permissions
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 8. Bảng rooms
-- ===============================
CREATE TABLE rooms
(
    id          SERIAL PRIMARY KEY,
    room_code   TEXT      NOT NULL UNIQUE,
    room_name   TEXT      NOT NULL,
    is_private  BOOLEAN   NOT NULL,
    owner_id    INT       NOT NULL,
    status      TEXT      NOT NULL,
    max_players INT       NOT NULL,
    created_at  TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at  TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    FOREIGN KEY (owner_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TRIGGER update_rooms_timestamp
    BEFORE INSERT OR
UPDATE ON rooms
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 9. Bảng room_players
-- ===============================
CREATE TABLE room_players (
                              room_id INT NOT NULL,
                              user_id INT NOT NULL,
                              score INT NOT NULL,
                              time_taken INTERVAL NOT NULL,
                              PRIMARY KEY (room_id, user_id),
                              FOREIGN KEY (room_id) REFERENCES rooms(id) ON DELETE CASCADE,
                              FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                              created_at TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
                              updated_at TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh')
);

CREATE TRIGGER update_room_players_timestamp
    BEFORE INSERT OR
UPDATE ON room_players
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 10. Bảng room_settings
-- ===============================
CREATE TABLE room_settings (
                               room_id INT REFERENCES rooms(id) ON DELETE CASCADE,
                               setting_key TEXT NOT NULL,
                               setting_value TEXT NOT NULL,
                               PRIMARY KEY (room_id, setting_key)
);

-- ===============================
-- 11. Bảng questions
-- ===============================
CREATE TABLE topics (
                        id SERIAL PRIMARY KEY,
                        name TEXT NOT NULL UNIQUE
);

CREATE TABLE question_types (
                                id SERIAL PRIMARY KEY,
                                name TEXT NOT NULL UNIQUE
);

CREATE TABLE questions (
                           id SERIAL PRIMARY KEY,
                           question_text TEXT NOT NULL,
                           topic_id INT REFERENCES topics(id) ON DELETE SET NULL,
                           question_type_id INT REFERENCES question_types(id) ON DELETE SET NULL,
                           created_at TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
                           updated_at TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh')
);

CREATE TRIGGER update_questions_timestamp
    BEFORE INSERT OR
UPDATE ON questions
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 12. Bảng answers
-- ===============================
CREATE TABLE answers
(
    id          SERIAL PRIMARY KEY,
    question_id INT       NOT NULL,
    answer_text TEXT      NOT NULL,
    is_correct  BOOLEAN   NOT NULL,
    created_at  TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at  TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
);

CREATE TRIGGER update_answers_timestamp
    BEFORE INSERT OR
UPDATE ON answers
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 13. Bảng user_answers
-- ===============================
CREATE TABLE user_answers
(
    user_id     INT       NOT NULL,
    room_id     INT       NOT NULL,
    question_id INT       NOT NULL,
    answer_id   INT       NOT NULL,
    is_correct  BOOLEAN   NOT NULL,
    time_taken  INTERVAL  NOT NULL,
    created_at  TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at  TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    PRIMARY KEY (user_id, room_id, question_id),
    FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    FOREIGN KEY (room_id) REFERENCES rooms (id) ON DELETE CASCADE,
    FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE,
    FOREIGN KEY (answer_id) REFERENCES answers (id) ON DELETE CASCADE
);

CREATE TRIGGER update_user_answers_timestamp
    BEFORE INSERT OR UPDATE ON user_answers
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();

-- ===============================
-- 14. Bảng ranks
-- ===============================
CREATE TABLE ranks
(
    id           SERIAL PRIMARY KEY,
    user_id      INT       NOT NULL,
    total_score  INT       NOT NULL,
    games_played INT       NOT NULL,
    created_at   TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    updated_at   TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'),
    FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TRIGGER update_ranks_timestamp
    BEFORE INSERT OR
UPDATE ON ranks
    FOR EACH ROW
    EXECUTE FUNCTION update_timestamp();
