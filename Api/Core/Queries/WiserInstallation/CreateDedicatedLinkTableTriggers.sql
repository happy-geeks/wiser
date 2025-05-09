-- ----------------------------
-- Triggers structure for table {LinkType}_wiser_itemlink
-- ----------------------------
DROP TRIGGER IF EXISTS `{LinkType}_LinkInsert`;
CREATE TRIGGER `{LinkType}_LinkInsert` AFTER INSERT ON `{LinkType}_wiser_itemlink` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('ADD_LINK', '{LinkType}_wiser_itemlink', NEW.destination_item_id, IFNULL(@_username, USER()), CONCAT(IFNULL(NEW.`type`, '1'), ',', IFNULL(NEW.`ordering`, '0')), NULL, NEW.item_id);
    END IF;

    IF IFNULL(@performParentUpdate, FALSE) = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `{LinkType}_LinkUpdate`;
CREATE TRIGGER `{LinkType}_LinkUpdate` AFTER UPDATE ON `{LinkType}_wiser_itemlink` FOR EACH ROW BEGIN
    DECLARE updateChangeDate BOOL;

    SET updateChangeDate = FALSE;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`destination_item_id` <> OLD.`destination_item_id` THEN
        SET updateChangeDate = TRUE;
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', '{LinkType}_wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'destination_item_id', OLD.destination_item_id, NEW.destination_item_id);
    END IF;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`item_id` <> OLD.`item_id` THEN
        SET updateChangeDate = TRUE;
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', '{LinkType}_wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'item_id', OLD.item_id, NEW.item_id);
    END IF;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`type` <> OLD.`type` THEN
        SET updateChangeDate = TRUE;
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', '{LinkType}_wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'type', OLD.type, NEW.type);
    END IF;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`ordering` <> OLD.`ordering` THEN
        SET updateChangeDate = TRUE;
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', '{LinkType}_wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'ordering', OLD.ordering, NEW.ordering);
    END IF;

    IF IFNULL(@performParentUpdate, FALSE) = TRUE AND updateChangeDate = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `{LinkType}_linkDelete`;
CREATE TRIGGER `{LinkType}_linkDelete` AFTER DELETE ON `{LinkType}_wiser_itemlink` FOR EACH ROW BEGIN
    DELETE FROM {LinkType}_wiser_itemlinkdetail WHERE itemlink_id = OLD.id;
    DELETE FROM {LinkType}_wiser_itemfile WHERE itemlink_id = OLD.id;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('REMOVE_LINK', '{LinkType}_wiser_itemlink', OLD.destination_item_id, IFNULL(@_username, USER()), OLD.`type`, OLD.item_id, NULL);
    END IF;

    IF IFNULL(@performParentUpdate, FALSE) = TRUE THEN
        IF (OLD.`item_id` IS NOT NULL AND OLD.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                OLD.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = OLD.`item_id`;
    END IF;
END;
-- ----------------------------
-- Triggers structure for table {LinkType}_wiser_itemlinkdetail
-- ----------------------------
DROP TRIGGER IF EXISTS `{LinkType}_LinkDetailInsert`;
CREATE TRIGGER `{LinkType}_LinkDetailInsert` AFTER INSERT ON `{LinkType}_wiser_itemlinkdetail` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, target_id, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEMLINKDETAIL', '{LinkType}_wiser_itemlinkdetail', NEW.id, NEW.itemlink_id, IFNULL(@_username, USER()), NEW.`key`, '', CONCAT_WS('', NEW.`value`, NEW.`long_value`), NEW.language_code, NEW.groupname);
    END IF;
END;

DROP TRIGGER IF EXISTS `{LinkType}_LinkDetailUpdate`;
CREATE TRIGGER `{LinkType}_LinkDetailUpdate` AFTER UPDATE ON `{LinkType}_wiser_itemlinkdetail` FOR EACH ROW BEGIN
    DECLARE oldValue MEDIUMTEXT;
    DECLARE newValue MEDIUMTEXT;

    SET oldValue = CONCAT_WS('', OLD.`value`, OLD.`long_value`);
    SET newValue = CONCAT_WS('', NEW.`value`, NEW.`long_value`);

    IF OLD.`key` <> NEW.`key` THEN
        IF IFNULL(@saveHistory, TRUE) = TRUE THEN
            INSERT INTO wiser_history (action, tablename, changed_by, target_id, item_id, field, oldvalue, newvalue)
            VALUES ('UPDATE_ITEMLINKDETAIL', '{LinkType}_wiser_itemlinkdetail', IFNULL(@_username, USER()), OLD.id, OLD.itemlink_id, 'key', OLD.`key`, NEW.`key`);
        END IF;
    END IF;

    IF OLD.`language_code` <> NEW.`language_code` THEN
        IF IFNULL(@saveHistory, TRUE) = TRUE THEN
            INSERT INTO wiser_history (action, tablename, changed_by, target_id, item_id, field, oldvalue, newvalue)
            VALUES ('UPDATE_ITEMLINKDETAIL', '{LinkType}_wiser_itemlinkdetail', IFNULL(@_username, USER()), OLD.id, OLD.itemlink_id, 'language_code', OLD.`language_code`, NEW.`language_code`);
        END IF;
    END IF;

    IF OLD.`groupname` <> NEW.`groupname` THEN
        IF IFNULL(@saveHistory, TRUE) = TRUE THEN
            INSERT INTO wiser_history (action, tablename, changed_by, target_id, item_id, field, oldvalue, newvalue)
            VALUES ('UPDATE_ITEMLINKDETAIL', '{LinkType}_wiser_itemlinkdetail', IFNULL(@_username, USER()), OLD.id, OLD.itemlink_id, 'groupname', OLD.`groupname`, NEW.`groupname`);
        END IF;
    END IF;

    IF oldvalue <> newValue THEN
        IF IFNULL(@saveHistory, TRUE) = TRUE THEN
            INSERT INTO wiser_history (action, tablename, target_id, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
            VALUES ('UPDATE_ITEMLINKDETAIL', '{LinkType}_wiser_itemlinkdetail', NEW.`id`, NEW.`itemlink_id`, IFNULL(@_username, USER()), NEW.`key`, oldValue, newValue, NEW.`language_code`, NEW.`groupname`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkDetailDelete`;
CREATE TRIGGER `LinkDetailDelete` AFTER DELETE ON `{LinkType}_wiser_itemlinkdetail` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, target_id, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEMLINKDETAIL', '{LinkType}_wiser_itemlinkdetail', OLD.id, OLD.itemlink_id, IFNULL(@_username, USER()), OLD.`key`, CONCAT_WS('', OLD.`value`, OLD.`long_value`), '', OLD.language_code, OLD.groupname);
    END IF;
END;

-- ----------------------------
-- Triggers structure for table {LinkType}_wiser_itemfile
-- ----------------------------
DROP TRIGGER IF EXISTS `{LinkType}_FileInsert`;
CREATE TRIGGER `{LinkType}_FileInsert` AFTER INSERT ON `{LinkType}_wiser_itemfile` FOR EACH ROW BEGIN
    DECLARE prevLinkedItemId BIGINT;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('ADD_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), IFNULL(NEW.property_name, ''), IF(IFNULL(NEW.item_id, 0) > 0, 'item_id', 'itemlink_id'), IF(IFNULL(NEW.item_id, 0) > 0, NEW.item_id, NEW.itemlink_id));

        IF IFNULL(NEW.content_type, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_type', NULL, NEW.content_type);
        END IF;

        IF NEW.content IS NOT NULL THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_length', '0 bytes', CONCAT(FORMAT(OCTET_LENGTH(NEW.content), 0, 'nl-NL'), ' bytes'));
        END IF;

        IF IFNULL(NEW.content_url, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_url', NULL, NEW.content_url);
        END IF;

        IF IFNULL(NEW.file_name, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'file_name', NULL, NEW.file_name);
        END IF;

        IF IFNULL(NEW.extension, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'extension', NULL, NEW.extension);
        END IF;

        IF IFNULL(NEW.title, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'title', NULL, NEW.title);
        END IF;

        IF IFNULL(NEW.property_name, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'property_name', NULL, NEW.property_name);
        END IF;

        IF IFNULL(NEW.ordering, 0) <> 0 THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'ordering', NULL, NEW.ordering);
        END IF;

        IF IFNULL(NEW.protected, 0) <> 0 THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'protected', NULL, NEW.protected);
        END IF;

        IF IFNULL(NEW.extra_data, 0) <> 0 THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'extra_data', NULL, NEW.extra_data);
        END IF;
    END IF;

    IF IFNULL(@performParentUpdate, FALSE) = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `{LinkType}_FileUpdate`;
CREATE TRIGGER `{LinkType}_FileUpdate` AFTER UPDATE ON `{LinkType}_wiser_itemfile` FOR EACH ROW BEGIN
    DECLARE updateChangeDate BOOL;

    SET updateChangeDate = FALSE;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        IF NEW.item_id <> OLD.item_id THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'item_id', OLD.item_id, NEW.item_id);
        END IF;

        IF NEW.content_type <> OLD.content_type THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_type', OLD.content_type, NEW.content_type);
        END IF;

        IF NEW.content <> OLD.content THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_length', CONCAT(FORMAT(OCTET_LENGTH(OLD.content), 0, 'nl-NL'), ' bytes'), CONCAT(FORMAT(OCTET_LENGTH(NEW.content), 0, 'nl-NL'), ' bytes'));
        END IF;

        IF NEW.content_url <> OLD.content_url THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_url', OLD.content_url, NEW.content_url);
        END IF;

        IF NEW.file_name <> OLD.file_name THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'file_name', OLD.file_name, NEW.file_name);
        END IF;

        IF NEW.extension <> OLD.extension THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'extension', OLD.extension, NEW.extension);
        END IF;

        IF NEW.title <> OLD.title THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'title', OLD.title, NEW.title);
        END IF;

        IF NEW.property_name <> OLD.property_name THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'property_name', OLD.property_name, NEW.property_name);
        END IF;

        IF NEW.itemlink_id <> OLD.itemlink_id THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'itemlink_id', OLD.itemlink_id, NEW.itemlink_id);
        END IF;

        IF NEW.ordering <> OLD.ordering THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'ordering', OLD.ordering, NEW.ordering);
        END IF;

        IF NEW.protected <> OLD.protected THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'protected', OLD.protected, NEW.protected);
        END IF;

        IF NEW.extra_data <> OLD.extra_data THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'extra_data', OLD.extra_data, NEW.extra_data);
        END IF;
    END IF;

    IF IFNULL(@performParentUpdate, FALSE) = TRUE AND updateChangeDate = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `{LinkType}_FileDelete`;
CREATE TRIGGER `{LinkType}_FileDelete` AFTER DELETE ON `{LinkType}_wiser_itemfile` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_FILE', '{LinkType}_wiser_itemfile', OLD.id, IFNULL(@_username, USER()), IFNULL(OLD.property_name, ''), IF(IFNULL(OLD.item_id, 0) > 0, 'item_id', 'itemlink_id'), IF(IFNULL(OLD.item_id, 0) > 0, OLD.item_id, OLD.itemlink_id));
    END IF;

    IF IFNULL(@performParentUpdate, FALSE) = TRUE THEN
        IF (OLD.`item_id` IS NOT NULL AND OLD.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                OLD.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = OLD.`item_id`;
    END IF;
END;