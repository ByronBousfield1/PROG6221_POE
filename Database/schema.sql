-- Cybersecurity Awareness Bot - Part 3 database schema
-- The application creates this automatically on startup, but you can also
-- run this manually in phpMyAdmin or the MySQL command line.

CREATE DATABASE IF NOT EXISTS cyberbot;
USE cyberbot;

CREATE TABLE IF NOT EXISTS tasks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    reminder_date DATETIME NULL,
    is_completed TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
