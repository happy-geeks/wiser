-- These are triggers for dedicated item tables, such as basket_wiser_item, basket_wiser_itemdetail etc.
-- These triggers contain the value '{tablePrefix}' that should be replaced in code with the proper prefix, before executing the queries.

-- ----------------------------
-- Triggers structure for table wiser_item
-- ----------------------------
DROP TRIGGER IF EXISTS `{tablePrefix}ItemInsert`;
CREATE TRIGGER `{tablePrefix}ItemInsert` AFTER INSERT ON `{tablePrefix}wiser_item` FOR EACH ROW
BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CREATE_ITEM','{tablePrefix}wiser_item', NEW.id, IFNULL(@_username, USER()), '', '', '');

        IF IFNULL(NEW.`unique_uuid`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'unique_uuid',NULL,NEW.`unique_uuid`);
        END IF;

        IF IFNULL(NEW.`entity_type`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'entity_type',NULL,NEW.`entity_type`);
        END IF;

        IF IFNULL(NEW.`moduleid`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'moduleid',NULL,NEW.`moduleid`);
        END IF;

        IF IFNULL(NEW.`published_environment`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'published_environment',NULL,NEW.`published_environment`);
        END IF;

        IF IFNULL(NEW.`readonly`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'readonly',NULL,NEW.`readonly`);
        END IF;

        IF IFNULL(NEW.`title`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'title',NULL,NEW.`title`);
        END IF;

        IF IFNULL(NEW.`original_item_id`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'original_item_id',NULL,NEW.`original_item_id`);
        END IF;

        IF IFNULL(NEW.`parent_item_id`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'parent_item_id',NULL,NEW.`parent_item_id`);
        END IF;

        IF IFNULL(NEW.`ordering`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'ordering',NULL,NEW.`ordering`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `{tablePrefix}ItemUpdate`;
CREATE TRIGGER `{tablePrefix}ItemUpdate` AFTER UPDATE ON `{tablePrefix}wiser_item` FOR EACH ROW
BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        IF IFNULL(NEW.`unique_uuid`, '') <> IFNULL(OLD.`unique_uuid`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'unique_uuid',OLD.`unique_uuid`,NEW.`unique_uuid`);
        END IF;

        IF IFNULL(NEW.`entity_type`, '') <> IFNULL(OLD.`entity_type`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'entity_type',OLD.`entity_type`,NEW.`entity_type`);
        END IF;

        IF IFNULL(NEW.`moduleid`, '') <> IFNULL(OLD.`moduleid`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'moduleid',OLD.`moduleid`,NEW.`moduleid`);
        END IF;

        IF IFNULL(NEW.`published_environment`, '') <> IFNULL(OLD.`published_environment`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'published_environment',OLD.`published_environment`,NEW.`published_environment`);
        END IF;

        IF IFNULL(NEW.`readonly`, '') <> IFNULL(OLD.`readonly`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'readonly',OLD.`readonly`,NEW.`readonly`);
        END IF;

        IF IFNULL(NEW.`title`, '') <> IFNULL(OLD.`title`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'title',OLD.`title`,NEW.`title`);
        END IF;

        IF IFNULL(NEW.`original_item_id`, '') <> IFNULL(OLD.`original_item_id`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'original_item_id',OLD.`original_item_id`,NEW.`original_item_id`);
        END IF;

        IF IFNULL(NEW.`parent_item_id`, '') <> IFNULL(OLD.`parent_item_id`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'parent_item_id',OLD.`parent_item_id`,NEW.`parent_item_id`);
        END IF;

        IF IFNULL(NEW.`ordering`, '') <> IFNULL(OLD.`ordering`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','{tablePrefix}wiser_item',NEW.`id`,IFNULL(@_username, USER()),'ordering',OLD.`ordering`,NEW.`ordering`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `{tablePrefix}ItemDelete`;
CREATE TRIGGER `{tablePrefix}ItemDelete` AFTER DELETE ON `{tablePrefix}wiser_item` FOR EACH ROW
BEGIN
    DELETE FROM `{tablePrefix}wiser_itemdetail` WHERE item_id = OLD.id;
    DELETE FROM `{tablePrefix}wiser_itemfile` WHERE item_id = OLD.id;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_ITEM','{tablePrefix}wiser_item', OLD.id, IFNULL(@_username, USER()), OLD.entity_type, '', '');
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_itemdetail
-- ----------------------------
DROP TRIGGER IF EXISTS `{tablePrefix}DetailInsert`;
CREATE TRIGGER `{tablePrefix}DetailInsert` AFTER INSERT ON `{tablePrefix}wiser_itemdetail` FOR EACH ROW BEGIN

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEM','{tablePrefix}wiser_itemdetail', NEW.item_id, IFNULL(@_username, USER()), NEW.`key`, '', CONCAT_WS('', NEW.`value`, NEW.`long_value`), NEW.language_code, NEW.groupname);
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                       NEW.`item_id`,
                       '{tablePrefix}wiser_item',
                       NOW(),
                       IFNULL(@_username, USER())
                   );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `{tablePrefix}DetailUpdate`;
CREATE TRIGGER `{tablePrefix}DetailUpdate` AFTER UPDATE ON `{tablePrefix}wiser_itemdetail` FOR EACH ROW BEGIN
    DECLARE oldValue MEDIUMTEXT;
    DECLARE newValue MEDIUMTEXT;
    
    SET oldValue = CONCAT_WS('', OLD.`value`, OLD.`long_value`);
    SET newValue = CONCAT_WS('', NEW.`value`, NEW.`long_value`);

    IF oldValue <> newValue THEN
        IF IFNULL(@saveHistory, TRUE) = TRUE THEN
            IF oldValue <> newValue THEN
                INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
                VALUES ('UPDATE_ITEM', '{tablePrefix}wiser_itemdetail', NEW.item_id, IFNULL(@_username, USER()), NEW.`key`, oldValue, newValue, NEW.language_code, NEW.groupname);
            END IF;
        END IF;

        IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
            IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
                INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
                VALUES (
                           NEW.`item_id`,
                           '{tablePrefix}wiser_item',
                           NOW(),
                           IFNULL(@_username, USER())
                       );
            END IF;

            SET @previousItemId = NEW.`item_id`;
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `{tablePrefix}DetailDelete`;
CREATE TRIGGER `{tablePrefix}DetailDelete` AFTER DELETE ON `{tablePrefix}wiser_itemdetail` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEM', '{tablePrefix}wiser_itemdetail', OLD.item_id, IFNULL(@_username, USER()), OLD.`key`, CONCAT_WS('', OLD.`value`, OLD.`long_value`), '', OLD.language_code, OLD.groupname);
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (OLD.`item_id` IS NOT NULL AND OLD.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                       OLD.`item_id`,
                       '{tablePrefix}wiser_item',
                       NOW(),
                       IFNULL(@_username, USER())
                   );
        END IF;

        SET @previousItemId = OLD.`item_id`;
    END IF;
END;
-- ----------------------------
-- Triggers structure for table wiser_itemfile
-- ----------------------------
DROP TRIGGER IF EXISTS `{tablePrefix}FileInsert`;
CREATE TRIGGER `{tablePrefix}FileInsert` AFTER INSERT ON `{tablePrefix}wiser_itemfile` FOR EACH ROW BEGIN
    DECLARE prevLinkedItemId BIGINT;
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('ADD_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), IFNULL(NEW.property_name, ''), IF(IFNULL(NEW.item_id, 0) > 0, 'item_id', 'itemlink_id'), IF(IFNULL(NEW.item_id, 0) > 0, NEW.item_id, NEW.itemlink_id));

        IF IFNULL(NEW.content_type, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_type', NULL, NEW.content_type);
        END IF;

        IF NEW.content IS NOT NULL THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_length', '0 bytes', CONCAT(FORMAT(OCTET_LENGTH(NEW.content), 0, 'nl-NL'), ' bytes'));
        END IF;

        IF IFNULL(NEW.content_url, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_url', NULL, NEW.content_url);
        END IF;

        IF IFNULL(NEW.width, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'width', NULL, NEW.width);
        END IF;

        IF IFNULL(NEW.height, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'height', NULL, NEW.height);
        END IF;

        IF IFNULL(NEW.file_name, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'file_name', NULL, NEW.file_name);
        END IF;

        IF IFNULL(NEW.extension, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'extension', NULL, NEW.extension);
        END IF;

        IF IFNULL(NEW.title, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'title', NULL, NEW.title);
        END IF;

        IF IFNULL(NEW.property_name, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'property_name', NULL, NEW.property_name);
        END IF;

        IF IFNULL(NEW.protected, 0) <> 0 THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'protected', NULL, NEW.protected);
        END IF;

        IF IFNULL(NEW.ordering, 0) <> 0 THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'ordering', NULL, NEW.ordering);
        END IF;

        IF IFNULL(NEW.extra_data, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'extra_data', NULL, NEW.extra_data);
        END IF;
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                       NEW.`item_id`,
                       '{tablePrefix}wiser_item',
                       NOW(),
                       IFNULL(@_username, USER())
                   );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `{tablePrefix}FileUpdate`;
CREATE TRIGGER `{tablePrefix}FileUpdate` AFTER UPDATE ON `{tablePrefix}wiser_itemfile` FOR EACH ROW BEGIN
    DECLARE updateChangeDate BOOL;
    
    SET updateChangeDate = FALSE;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        IF NEW.item_id <> OLD.item_id THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'item_id', OLD.item_id, NEW.item_id);
        END IF;

        IF NEW.content_type <> OLD.content_type THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_type', OLD.content_type, NEW.content_type);
        END IF;

        IF NEW.content <> OLD.content THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_length', CONCAT(FORMAT(OCTET_LENGTH(OLD.content), 0, 'nl-NL'), ' bytes'), CONCAT(FORMAT(OCTET_LENGTH(NEW.content), 0, 'nl-NL'), ' bytes'));
        END IF;

        IF NEW.content_url <> OLD.content_url THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_url', OLD.content_url, NEW.content_url);
        END IF;

        IF NEW.width <> OLD.width THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'width', OLD.width, NEW.width);
        END IF;

        IF NEW.height <> OLD.height THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'height', OLD.height, NEW.height);
        END IF;

        IF NEW.file_name <> OLD.file_name THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'file_name', OLD.file_name, NEW.file_name);
        END IF;

        IF NEW.extension <> OLD.extension THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'extension', OLD.extension, NEW.extension);
        END IF;

        IF NEW.title <> OLD.title THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'title', OLD.title, NEW.title);
        END IF;

        IF NEW.property_name <> OLD.property_name THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'property_name', OLD.property_name, NEW.property_name);
        END IF;

        IF NEW.itemlink_id <> OLD.itemlink_id THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'itemlink_id', OLD.itemlink_id, NEW.itemlink_id);
        END IF;

        IF NEW.protected <> OLD.protected THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'protected', OLD.protected, NEW.protected);
        END IF;

        IF NEW.ordering <> OLD.ordering THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'ordering', OLD.ordering, NEW.ordering);
        END IF;

        IF NEW.extra_data <> OLD.extra_data THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'extra_data', OLD.extra_data, NEW.extra_data);
        END IF;
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE AND updateChangeDate = TRUE  THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                       NEW.`item_id`,
                       '{tablePrefix}wiser_item',
                       NOW(),
                       IFNULL(@_username, USER())
                   );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `{tablePrefix}FileDelete`;
CREATE TRIGGER `{tablePrefix}FileDelete` AFTER DELETE ON `{tablePrefix}wiser_itemfile` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_FILE', '{tablePrefix}wiser_itemfile', OLD.id, IFNULL(@_username, USER()), IFNULL(OLD.property_name, ''), IF(IFNULL(OLD.item_id, 0) > 0, 'item_id', 'itemlink_id'), IF(IFNULL(OLD.item_id, 0) > 0, OLD.item_id, OLD.itemlink_id));
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (OLD.`item_id` IS NOT NULL AND OLD.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                       OLD.`item_id`,
                       '{tablePrefix}wiser_item',
                       NOW(),
                       IFNULL(@_username, USER())
                   );
        END IF;

        SET @previousItemId = OLD.`item_id`;
    END IF;
END;