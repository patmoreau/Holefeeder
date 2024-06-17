CREATE TABLE IF NOT EXISTS users
(
    id        UUID  NOT NULL PRIMARY KEY,
    inactive  BOOL  NOT NULL
);

CREATE TABLE IF NOT EXISTS user_identities
(
    user_id             UUID           NOT NULL,
    identity_object_id  NVARCHAR(255)  NOT NULL,
    inactive            BOOL           NOT NULL,
    PRIMARY KEY (user_id, identity_object_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS identity_object_id_idx ON user_identities (identity_object_id);

-- Step 1: Generate UUIDs for each distinct user_id from accounts
CREATE TEMPORARY TABLE temp_users AS
SELECT DISTINCT user_id, UUID() as new_id
FROM accounts;

-- Step 2: Insert these UUIDs into the users table
INSERT INTO users (id, inactive)
SELECT new_id, FALSE
FROM temp_users;

-- Step 3: Insert the user_id from accounts into user_identities
INSERT INTO user_identities (user_id, identity_object_id, inactive)
SELECT new_id, user_id, FALSE
FROM temp_users;

ALTER TABLE accounts
    ADD CONSTRAINT fk_account_user
        FOREIGN KEY (user_id) REFERENCES users(id)
            ON DELETE RESTRICT
            ON UPDATE RESTRICT;
ALTER TABLE categories
    ADD CONSTRAINT fk_category_user
        FOREIGN KEY (user_id) REFERENCES users(id)
            ON DELETE RESTRICT
            ON UPDATE RESTRICT;
ALTER TABLE cashflows
    ADD CONSTRAINT fk_cashflow_user
        FOREIGN KEY (user_id) REFERENCES users(id)
            ON DELETE RESTRICT
            ON UPDATE RESTRICT;
ALTER TABLE transactions
    ADD CONSTRAINT fk_transaction_user
        FOREIGN KEY (user_id) REFERENCES users(id)
            ON DELETE RESTRICT
            ON UPDATE RESTRICT;
ALTER TABLE store_items
    ADD CONSTRAINT fk_store_item_user
        FOREIGN KEY (user_id) REFERENCES users(id)
            ON DELETE RESTRICT
            ON UPDATE RESTRICT;