CREATE DATABASE IF NOT EXISTS draw_together
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE draw_together;

-- 1. USERS: lưu tài khoản người dùng.
-- Không bao giờ lưu mật khẩu thật. Chỉ lưu password_hash bằng BCrypt.
CREATE TABLE IF NOT EXISTS users (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    UNIQUE KEY uq_users_username (username)
);

-- 2. ROOMS: lưu thông tin phòng vẽ.
-- id dùng CHAR(36) để lưu Guid/UUID dạng text.
CREATE TABLE IF NOT EXISTS rooms (
    id CHAR(36) NOT NULL,
    name VARCHAR(100) NOT NULL,
    owner_user_id BIGINT UNSIGNED NOT NULL,
    max_members INT UNSIGNED NOT NULL DEFAULT 10,
    is_closed TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    KEY idx_rooms_owner_user_id (owner_user_id),
    KEY idx_rooms_is_closed (is_closed),

    CONSTRAINT fk_rooms_owner
        FOREIGN KEY (owner_user_id) REFERENCES users(id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

-- 3. ROOM_MEMBERS: lưu ai đã/đang tham gia phòng nào.
-- left_at = NULL nghĩa là user vẫn đang ở trong phòng.
CREATE TABLE IF NOT EXISTS room_members (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    room_id CHAR(36) NOT NULL,
    user_id BIGINT UNSIGNED NOT NULL,
    joined_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    left_at DATETIME NULL,

    PRIMARY KEY (id),
    KEY idx_room_members_room_id (room_id),
    KEY idx_room_members_user_id (user_id),
    KEY idx_room_members_active (room_id, left_at),

    CONSTRAINT fk_room_members_room
        FOREIGN KEY (room_id) REFERENCES rooms(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_room_members_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- 4. DRAW_HISTORY: lưu các thao tác vẽ để sync canvas khi user mới join phòng.
-- payload_json lưu dữ liệu stroke/shape/clear dưới dạng JSON để dễ mở rộng.
CREATE TABLE IF NOT EXISTS draw_history (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    room_id CHAR(36) NOT NULL,
    user_id BIGINT UNSIGNED NOT NULL,
    action_type VARCHAR(40) NOT NULL,
    payload_json JSON NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    KEY idx_draw_history_room_order (room_id, id),
    KEY idx_draw_history_user_id (user_id),

    CONSTRAINT fk_draw_history_room
        FOREIGN KEY (room_id) REFERENCES rooms(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_draw_history_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- 5. CHAT_HISTORY: lưu chat theo phòng.
-- Nếu nhóm chưa làm chat thì bảng này vẫn có thể để lại, không ảnh hưởng.
CREATE TABLE IF NOT EXISTS chat_history (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    room_id CHAR(36) NOT NULL,
    user_id BIGINT UNSIGNED NOT NULL,
    message TEXT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    KEY idx_chat_history_room_order (room_id, id),
    KEY idx_chat_history_user_id (user_id),

    CONSTRAINT fk_chat_history_room
        FOREIGN KEY (room_id) REFERENCES rooms(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT fk_chat_history_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);
