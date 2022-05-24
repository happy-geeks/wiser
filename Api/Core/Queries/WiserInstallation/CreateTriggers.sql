SET NAMES utf8mb4 COLLATE utf8mb4_general_ci;

# Drop old triggers with old names, originally we had trigger names that weren't very consistent and sometimes not logical either, so we changed them.
DROP TRIGGER IF EXISTS `CreateItem`;
DROP TRIGGER IF EXISTS `UpdateItem`;
DROP TRIGGER IF EXISTS `DeleteItem`;
DROP TRIGGER IF EXISTS `toevoeging`;
DROP TRIGGER IF EXISTS `wijziging`;
DROP TRIGGER IF EXISTS `verwijdering`;
DROP TRIGGER IF EXISTS `On_insert`;
DROP TRIGGER IF EXISTS `On_change`;
DROP TRIGGER IF EXISTS `On_remove`;
DROP TRIGGER IF EXISTS `addhistory`;
DROP TRIGGER IF EXISTS `updatehistory`;
DROP TRIGGER IF EXISTS `removehistory`;
DROP TRIGGER IF EXISTS `On_insert_file`;
DROP TRIGGER IF EXISTS `On_change_file`;
DROP TRIGGER IF EXISTS `On_remove_file`;

-- ----------------------------
-- Triggers structure for table wiser_entityproperty
-- ----------------------------
DROP TRIGGER IF EXISTS `EntityPropertyInsert`;
CREATE TRIGGER `EntityPropertyInsert` AFTER INSERT ON `wiser_entityproperty` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
    VALUES ('INSERT_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'module_id,entity_name,property_name','',CONCAT_WS(',',NEW.module_id,NEW.entity_name,NEW.property_name));
END;

DROP TRIGGER IF EXISTS `EntityPropertyUpdate`;
CREATE TRIGGER `EntityPropertyUpdate` AFTER UPDATE ON `wiser_entityproperty` FOR EACH ROW BEGIN
    IF NEW.`property_name` <> OLD.`property_name` THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'property_name',OLD.`property_name`,NEW.`property_name`);
    END IF;

    IF IFNULL(NEW.`options`, '') <> IFNULL(OLD.`options`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'options',OLD.`options`,NEW.`options`);
    END IF;

    IF IFNULL(NEW.`data_query`, '') <> IFNULL(OLD.`data_query`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'data_query',OLD.`data_query`,NEW.`data_query`);
    END IF;

    IF IFNULL(NEW.`action_query`, '') <> IFNULL(OLD.`action_query`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'action_query',OLD.`action_query`,NEW.`action_query`);
    END IF;

    IF IFNULL(NEW.`search_query`, '') <> IFNULL(OLD.`search_query`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'search_query',OLD.`search_query`,NEW.`search_query`);
    END IF;

    IF IFNULL(NEW.`search_count_query`, '') <> IFNULL(OLD.`search_count_query`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'search_count_query',OLD.`search_count_query`,NEW.`search_count_query`);
    END IF;

    IF IFNULL(NEW.`grid_delete_query`, '') <> IFNULL(OLD.`grid_delete_query`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'grid_delete_query',OLD.`grid_delete_query`,NEW.`grid_delete_query`);
    END IF;

    IF IFNULL(NEW.`grid_insert_query`, '') <> IFNULL(OLD.`grid_insert_query`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'grid_insert_query',OLD.`grid_insert_query`,NEW.`grid_insert_query`);
    END IF;

    IF IFNULL(NEW.`grid_update_query`, '') <> IFNULL(OLD.`grid_update_query`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'grid_update_query',OLD.`grid_update_query`,NEW.`grid_update_query`);
    END IF;

    IF IFNULL(NEW.`custom_script`, '') <> IFNULL(OLD.`custom_script`, '') THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
        VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty',NEW.id,IFNULL(@_username, USER()),'custom_script',OLD.`custom_script`,NEW.`custom_script`);
    END IF;
END;

DROP TRIGGER IF EXISTS `EntityPropertyDelete`;
CREATE TRIGGER `EntityPropertyDelete` AFTER DELETE ON `wiser_entityproperty` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
    VALUES ('DELETE_ENTITYPROPERTY','wiser_entityproperty',OLD.id,IFNULL(@_username, USER()),'module_id,entity_name,property_name',CONCAT_WS(',',OLD.module_id,OLD.entity_name,OLD.property_name),'');
END;

-- ----------------------------
-- Triggers structure for table wiser_item
-- ----------------------------
DROP TRIGGER IF EXISTS `ItemInsert`;
CREATE TRIGGER `ItemInsert` AFTER INSERT ON `wiser_item` FOR EACH ROW
BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CREATE_ITEM','wiser_item', NEW.id, IFNULL(@_username, USER()), '', '', '');
        
        IF IFNULL(NEW.`unique_uuid`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'unique_uuid',NULL,NEW.`unique_uuid`);
        END IF;

        IF IFNULL(NEW.`entity_type`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'entity_type',NULL,NEW.`entity_type`);
        END IF;

        IF IFNULL(NEW.`moduleid`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'moduleid',NULL,NEW.`moduleid`);
        END IF;

        IF IFNULL(NEW.`published_environment`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'published_environment',NULL,NEW.`published_environment`);
        END IF;

        IF IFNULL(NEW.`readonly`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'readonly',NULL,NEW.`readonly`);
        END IF;

        IF IFNULL(NEW.`title`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'title',NULL,NEW.`title`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `ItemUpdate`;
CREATE TRIGGER `ItemUpdate` AFTER UPDATE ON `wiser_item` FOR EACH ROW
BEGIN
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
		IF IFNULL(NEW.`unique_uuid`, '') <> IFNULL(OLD.`unique_uuid`, '') THEN
			INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
			VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'unique_uuid',OLD.`unique_uuid`,NEW.`unique_uuid`);
		END IF;

		IF IFNULL(NEW.`entity_type`, '') <> IFNULL(OLD.`entity_type`, '') THEN
			INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
			VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'entity_type',OLD.`entity_type`,NEW.`entity_type`);
		END IF;

		IF IFNULL(NEW.`moduleid`, '') <> IFNULL(OLD.`moduleid`, '') THEN
			INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
			VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'moduleid',OLD.`moduleid`,NEW.`moduleid`);
		END IF;

		IF IFNULL(NEW.`published_environment`, '') <> IFNULL(OLD.`published_environment`, '') THEN
			INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
			VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'published_environment',OLD.`published_environment`,NEW.`published_environment`);
		END IF;

		IF IFNULL(NEW.`readonly`, '') <> IFNULL(OLD.`readonly`, '') THEN
			INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
			VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'readonly',OLD.`readonly`,NEW.`readonly`);
		END IF;

		IF IFNULL(NEW.`title`, '') <> IFNULL(OLD.`title`, '') THEN
			INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
			VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'title',OLD.`title`,NEW.`title`);
		END IF;
	END IF;
END;

DROP TRIGGER IF EXISTS `ItemDelete`;
CREATE TRIGGER `ItemDelete` AFTER DELETE ON `wiser_item` FOR EACH ROW
BEGIN
	DELETE d.* FROM wiser_itemlink l JOIN wiser_itemlinkdetail AS d ON d.itemlink_id = l.id WHERE l.item_id = OLD.id OR l.destination_item_id = OLD.id;
	DELETE FROM wiser_itemlink WHERE (item_id = OLD.id OR destination_item_id = OLD.id) AND OLD.id > 0;
	DELETE FROM wiser_itemdetail WHERE item_id = OLD.id;
	DELETE FROM wiser_itemfile WHERE item_id = OLD.id;
	
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_ITEM','wiser_item', OLD.id, IFNULL(@_username, USER()), OLD.entity_type, '', '');
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_itemdetail
-- ----------------------------
DROP TRIGGER IF EXISTS `DetailInsert`;
CREATE TRIGGER `DetailInsert` AFTER INSERT ON `wiser_itemdetail` FOR EACH ROW BEGIN
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEM','wiser_itemdetail', NEW.item_id, IFNULL(@_username, USER()), NEW.`key`, '', CONCAT_WS('', NEW.`value`, NEW.`long_value`), NEW.language_code, NEW.groupname);
    END IF;
END;

DROP TRIGGER IF EXISTS `DetailUpdate`;
CREATE TRIGGER `DetailUpdate` AFTER UPDATE ON `wiser_itemdetail` FOR EACH ROW BEGIN
    DECLARE oldValue MEDIUMTEXT;
    DECLARE newValue MEDIUMTEXT;
	
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
		SET oldValue = CONCAT_WS('', OLD.`value`, OLD.`long_value`);
		SET newValue = CONCAT_WS('', NEW.`value`, NEW.`long_value`);
        IF oldValue <> newValue THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
            VALUES ('UPDATE_ITEM', 'wiser_itemdetail', NEW.item_id, IFNULL(@_username, USER()), NEW.`key`, oldValue, newValue, NEW.language_code, NEW.groupname);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `DetailDelete`;
CREATE TRIGGER `DetailDelete` AFTER DELETE ON `wiser_itemdetail` FOR EACH ROW BEGIN
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEM', 'wiser_itemdetail', OLD.item_id, IFNULL(@_username, USER()), OLD.`key`, CONCAT_WS('', OLD.`value`, OLD.`long_value`), '', OLD.language_code, OLD.groupname);
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_itemlink
-- ----------------------------
DROP TRIGGER IF EXISTS `LinkInsert`;
CREATE TRIGGER `LinkInsert` AFTER INSERT ON `wiser_itemlink` FOR EACH ROW BEGIN
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('ADD_LINK', 'wiser_itemlink', NEW.destination_item_id, IFNULL(@_username, USER()), CONCAT(IFNULL(NEW.`type`, '1'), ',', IFNULL(NEW.`ordering`, '0')), NULL, NEW.item_id);
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkUpdate`;
CREATE TRIGGER `LinkUpdate` AFTER UPDATE ON `wiser_itemlink` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`destination_item_id` <> OLD.`destination_item_id` THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', 'wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'destination_item_id', OLD.destination_item_id, NEW.destination_item_id);
    END IF;
    
    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`item_id` <> OLD.`item_id` THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', 'wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'item_id', OLD.item_id, NEW.item_id);
    END IF;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`type` <> OLD.`type` THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', 'wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'type', OLD.type, NEW.type);
    END IF;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`ordering` <> OLD.`ordering` THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', 'wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'ordering', OLD.ordering, NEW.ordering);
    END IF;
END;

DROP TRIGGER IF EXISTS `linkDelete`;
CREATE TRIGGER `linkDelete` AFTER DELETE ON `wiser_itemlink` FOR EACH ROW BEGIN
	DELETE FROM wiser_itemlinkdetail WHERE itemlink_id = OLD.id;
	DELETE FROM wiser_itemfile WHERE itemlink_id = OLD.id;
    
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
    	INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    	VALUES ('REMOVE_LINK', 'wiser_itemlink', OLD.destination_item_id, IFNULL(@_username, USER()), OLD.`type`, OLD.item_id, NULL);
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_itemlinkdetail
-- ----------------------------
DROP TRIGGER IF EXISTS `LinkDetailInsert`;
CREATE TRIGGER `LinkDetailInsert` AFTER INSERT ON `wiser_itemlinkdetail` FOR EACH ROW BEGIN
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEMLINKDETAIL','wiser_itemlinkdetail',NEW.itemlink_id,IFNULL(@_username, USER()),NEW.`key`,'',CONCAT_WS('',NEW.`value`,NEW.`long_value`), NEW.language_code, NEW.groupname);
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkDetailUpdate`;
CREATE TRIGGER `LinkDetailUpdate` AFTER UPDATE ON `wiser_itemlinkdetail` FOR EACH ROW BEGIN
    DECLARE oldValue MEDIUMTEXT;
    DECLARE newValue MEDIUMTEXT;
    
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        SET oldValue = CONCAT_WS('',OLD.`value`,OLD.`long_value`);
        SET newValue = CONCAT_WS('',NEW.`value`,NEW.`long_value`);
        IF newValue <> oldValue THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue, language_code, groupname)
            VALUES ('UPDATE_ITEMLINKDETAIL','wiser_itemlinkdetail',NEW.itemlink_id,IFNULL(@_username, USER()),NEW.`key`,oldValue,newValue, NEW.language_code, NEW.groupname);
        END IF;
	END IF;
END;

DROP TRIGGER IF EXISTS `LinkDetailDelete`;
CREATE TRIGGER `LinkDetailDelete` AFTER DELETE ON `wiser_itemlinkdetail` FOR EACH ROW BEGIN
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
    	INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue, language_code, groupname)
    	VALUES ('UPDATE_ITEMLINKDETAIL','wiser_itemlinkdetail',OLD.itemlink_id,IFNULL(@_username, USER()),OLD.`key`,CONCAT_WS('',OLD.`value`,OLD.`long_value`),'', OLD.language_code, OLD.groupname);
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_itemfile
-- ----------------------------
DROP TRIGGER IF EXISTS `FileInsert`;
CREATE TRIGGER `FileInsert` AFTER INSERT ON `wiser_itemfile` FOR EACH ROW BEGIN
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('ADD_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), IFNULL(NEW.property_name, ''), IF(IFNULL(NEW.item_id, 0) > 0, 'item_id', 'itemlink_id'), IF(IFNULL(NEW.item_id, 0) > 0, NEW.item_id, NEW.itemlink_id));

        IF IFNULL(NEW.content_type, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_type', NULL, NEW.content_type);
        END IF;

        IF NEW.content IS NOT NULL THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_length', '0 bytes', CONCAT(FORMAT(OCTET_LENGTH(NEW.content), 0, 'nl-NL'), ' bytes'));
        END IF;

        IF IFNULL(NEW.content_url, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_url', NULL, NEW.content_url);
        END IF;

        IF IFNULL(NEW.width, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'width', NULL, NEW.width);
        END IF;

        IF IFNULL(NEW.height, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'height', NULL, NEW.height);
        END IF;

        IF IFNULL(NEW.file_name, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'file_name', NULL, NEW.file_name);
        END IF;

        IF IFNULL(NEW.extension, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'extension', NULL, NEW.extension);
        END IF;

        IF IFNULL(NEW.title, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'title', NULL, NEW.title);
        END IF;

        IF IFNULL(NEW.property_name, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'property_name', NULL, NEW.property_name);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `FileUpdate`;
CREATE TRIGGER `FileUpdate` AFTER UPDATE ON `wiser_itemfile` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
		IF NEW.item_id <> OLD.item_id THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'item_id', OLD.item_id, NEW.item_id);
		END IF;
		
		IF NEW.content_type <> OLD.content_type THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_type', OLD.content_type, NEW.content_type);
		END IF;
		
		IF NEW.content <> OLD.content THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_length', CONCAT(FORMAT(OCTET_LENGTH(OLD.content), 0, 'nl-NL'), ' bytes'), CONCAT(FORMAT(OCTET_LENGTH(NEW.content), 0, 'nl-NL'), ' bytes'));
		END IF;
		
		IF NEW.content_url <> OLD.content_url THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_url', OLD.content_url, NEW.content_url);
		END IF;
		
		IF NEW.width <> OLD.width THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'width', OLD.width, NEW.width);
		END IF;
		
		IF NEW.height <> OLD.height THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'height', OLD.height, NEW.height);
		END IF;
		
		IF NEW.file_name <> OLD.file_name THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'file_name', OLD.file_name, NEW.file_name);
		END IF;
		
		IF NEW.extension <> OLD.extension THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'extension', OLD.extension, NEW.extension);
		END IF;
		
		IF NEW.title <> OLD.title THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'title', OLD.title, NEW.title);
		END IF;
		
		IF NEW.property_name <> OLD.property_name THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'property_name', OLD.property_name, NEW.property_name);
		END IF;
		
		IF NEW.itemlink_id <> OLD.itemlink_id THEN
			INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
			VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'itemlink_id', OLD.itemlink_id, NEW.itemlink_id);
		END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `FileDelete`;
CREATE TRIGGER `FileDelete` AFTER DELETE ON `wiser_itemfile` FOR EACH ROW BEGIN
	IF IFNULL(@saveHistory, TRUE) = TRUE THEN
    	INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), IFNULL(OLD.property_name, ''), IF(IFNULL(OLD.item_id, 0) > 0, 'item_id', 'itemlink_id'), IF(IFNULL(OLD.item_id, 0) > 0, OLD.item_id, OLD.itemlink_id));
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_module
-- ----------------------------
DROP TRIGGER IF EXISTS `ModuleInsert`;
CREATE TRIGGER `ModuleInsert` AFTER INSERT ON `wiser_module` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`custom_query`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'custom_query', NULL, NEW.`custom_query`);
    END IF;

    IF IFNULL(NEW.`count_query`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'count_query', NULL, NEW.`count_query`);
    END IF;

    IF IFNULL(NEW.`options`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'options', NULL, NEW.`options`);
    END IF;

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`icon`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'icon', NULL, NEW.`icon`);
    END IF;

    IF IFNULL(NEW.`color`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'color', NULL, NEW.`color`);
    END IF;

    IF IFNULL(NEW.`type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'type', NULL, NEW.`type`);
    END IF;

    IF IFNULL(NEW.`group`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'group', NULL, NEW.`group`);
    END IF;
END;

DROP TRIGGER IF EXISTS `ModuleUpdate`;
CREATE TRIGGER `ModuleUpdate` AFTER UPDATE ON `wiser_module` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`custom_query`, '') <> IFNULL(OLD.`custom_query`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'custom_query', OLD.`custom_query`, NEW.`custom_query`);
    END IF;

    IF IFNULL(NEW.`count_query`, '') <> IFNULL(OLD.`count_query`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'count_query', OLD.`count_query`, NEW.`count_query`);
    END IF;

    IF IFNULL(NEW.`options`, '') <> IFNULL(OLD.`options`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'options', OLD.`options`, NEW.`options`);
    END IF;

    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`icon`, '') <> IFNULL(OLD.`icon`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'icon', OLD.`icon`, NEW.`icon`);
    END IF;

    IF IFNULL(NEW.`color`, '') <> IFNULL(OLD.`color`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'color', OLD.`color`, NEW.`color`);
    END IF;

    IF IFNULL(NEW.`type`, '') <> IFNULL(OLD.`type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'type', OLD.`type`, NEW.`type`);
    END IF;

    IF IFNULL(NEW.`group`, '') <> IFNULL(OLD.`group`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'group', OLD.`group`, NEW.`group`);
    END IF;
END;

DROP TRIGGER IF EXISTS `ModuleDelete`;
CREATE TRIGGER `ModuleDelete` AFTER DELETE ON `wiser_module` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_MODULE', 'wiser_module', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_query
-- ----------------------------
DROP TRIGGER IF EXISTS `QueryInsert`;
CREATE TRIGGER `QueryInsert` AFTER INSERT ON `wiser_query` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`description`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'description', NULL, NEW.`description`);
    END IF;

    IF IFNULL(NEW.`query`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'query', NULL, NEW.`query`);
    END IF;

    IF IFNULL(NEW.`show_in_export_module`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'show_in_export_module', NULL, NEW.`show_in_export_module`);
    END IF;
END;

DROP TRIGGER IF EXISTS `QueryUpdate`;
CREATE TRIGGER `QueryUpdate` AFTER UPDATE ON `wiser_query` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`description`, '') <> IFNULL(OLD.`description`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'description', OLD.`description`, NEW.`description`);
    END IF;

    IF IFNULL(NEW.`query`, '') <> IFNULL(OLD.`query`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'query', OLD.`query`, NEW.`query`);
    END IF;

    IF IFNULL(NEW.`show_in_export_module`, '') <> IFNULL(OLD.`show_in_export_module`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'show_in_export_module', OLD.`show_in_export_module`, NEW.`show_in_export_module`);
    END IF;
END;

DROP TRIGGER IF EXISTS `QueryDelete`;
CREATE TRIGGER `QueryDelete` AFTER DELETE ON `wiser_query` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_QUERY', 'wiser_query', OLD.id, IFNULL(@_username, USER()), 'description', OLD.description, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_entity
-- ----------------------------
DROP TRIGGER IF EXISTS `EntityInsert`;
CREATE TRIGGER `EntityInsert` AFTER INSERT ON `wiser_entity` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`module_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'module_id', NULL, NEW.`module_id`);
    END IF;

    IF IFNULL(NEW.`accepted_childtypes`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'accepted_childtypes', NULL, NEW.`accepted_childtypes`);
    END IF;

    IF IFNULL(NEW.`icon`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon', NULL, NEW.`icon`);
    END IF;

    IF IFNULL(NEW.`icon_add`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon_add', NULL, NEW.`icon_add`);
    END IF;

    IF IFNULL(NEW.`icon_expanded`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon_expanded', NULL, NEW.`icon_expanded`);
    END IF;

    IF IFNULL(NEW.`show_in_tree_view`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', NULL, NEW.`show_in_tree_view`);
    END IF;

    IF IFNULL(NEW.`query_after_insert`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_after_insert', NULL, NEW.`query_after_insert`);
    END IF;

    IF IFNULL(NEW.`query_after_update`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_after_update', NULL, NEW.`query_after_update`);
    END IF;

    IF IFNULL(NEW.`query_before_update`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_before_update', NULL, NEW.`query_before_update`);
    END IF;

    IF IFNULL(NEW.`query_before_delete`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_before_delete', NULL, NEW.`query_before_delete`);
    END IF;

    IF IFNULL(NEW.`color`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'color', NULL, NEW.`color`);
    END IF;

    IF IFNULL(NEW.`show_in_search`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_in_search', NULL, NEW.`show_in_search`);
    END IF;

    IF IFNULL(NEW.`show_overview_tab`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_overview_tab', NULL, NEW.`show_overview_tab`);
    END IF;

    IF IFNULL(NEW.`save_title_as_seo`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'save_title_as_seo', NULL, NEW.`save_title_as_seo`);
    END IF;

    IF IFNULL(NEW.`api_after_insert`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_after_insert', NULL, NEW.`api_after_insert`);
    END IF;

    IF IFNULL(NEW.`api_after_update`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_after_update', NULL, NEW.`api_after_update`);
    END IF;

    IF IFNULL(NEW.`api_before_update`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_before_update', NULL, NEW.`api_before_update`);
    END IF;

    IF IFNULL(NEW.`api_before_delete`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_before_delete', NULL, NEW.`api_before_delete`);
    END IF;

    IF IFNULL(NEW.`show_title_field`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_title_field', NULL, NEW.`show_title_field`);
    END IF;

    IF IFNULL(NEW.`friendly_name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'friendly_name', NULL, NEW.`friendly_name`);
    END IF;

    IF IFNULL(NEW.`default_ordering`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'default_ordering', NULL, NEW.`default_ordering`);
    END IF;

    IF IFNULL(NEW.`save_history`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'save_history', NULL, NEW.`save_history`);
    END IF;

    IF IFNULL(NEW.`enable_multiple_environments`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'enable_multiple_environments', NULL, NEW.`enable_multiple_environments`);
    END IF;

    IF IFNULL(NEW.`use_dedicated_table`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'use_dedicated_table', NULL, NEW.`use_dedicated_table`);
    END IF;

    IF IFNULL(NEW.`dedicated_table_prefix`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'dedicated_table_prefix', NULL, NEW.`dedicated_table_prefix`);
    END IF;
END;

DROP TRIGGER IF EXISTS `EntityUpdate`;
CREATE TRIGGER `EntityUpdate` AFTER UPDATE ON `wiser_entity` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;
	
    IF IFNULL(NEW.`module_id`, '') <> IFNULL(OLD.`module_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'module_id', OLD.`module_id`, NEW.`module_id`);
    END IF;
	
    IF IFNULL(NEW.`accepted_childtypes`, '') <> IFNULL(OLD.`accepted_childtypes`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'accepted_childtypes', OLD.`accepted_childtypes`, NEW.`accepted_childtypes`);
    END IF;
	
    IF IFNULL(NEW.`icon`, '') <> IFNULL(OLD.`icon`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon', OLD.`icon`, NEW.`icon`);
    END IF;
	
    IF IFNULL(NEW.`icon_expanded`, '') <> IFNULL(OLD.`icon_expanded`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon_expanded', OLD.`icon_expanded`, NEW.`icon_expanded`);
    END IF;
	
    IF IFNULL(NEW.`icon_add`, '') <> IFNULL(OLD.`icon_add`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon_add', OLD.`icon_add`, NEW.`icon_add`);
    END IF;
	
    IF IFNULL(NEW.`show_in_tree_view`, '') <> IFNULL(OLD.`show_in_tree_view`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', OLD.`show_in_tree_view`, NEW.`show_in_tree_view`);
    END IF;
	
    IF IFNULL(NEW.`query_after_insert`, '') <> IFNULL(OLD.`query_after_insert`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_after_insert', OLD.`query_after_insert`, NEW.`query_after_insert`);
    END IF;
	
    IF IFNULL(NEW.`query_after_update`, '') <> IFNULL(OLD.`query_after_update`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_after_update', OLD.`query_after_update`, NEW.`query_after_update`);
    END IF;
	
    IF IFNULL(NEW.`query_before_update`, '') <> IFNULL(OLD.`query_before_update`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_before_update', OLD.`query_before_update`, NEW.`query_before_update`);
    END IF;
	
    IF IFNULL(NEW.`query_before_delete`, '') <> IFNULL(OLD.`query_before_delete`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_before_delete', OLD.`query_before_delete`, NEW.`query_before_delete`);
    END IF;
	
    IF IFNULL(NEW.`color`, '') <> IFNULL(OLD.`color`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'color', OLD.`color`, NEW.`color`);
    END IF;
	
    IF IFNULL(NEW.`show_in_search`, '') <> IFNULL(OLD.`show_in_search`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_in_search', OLD.`show_in_search`, NEW.`show_in_search`);
    END IF;
	
    IF IFNULL(NEW.`show_overview_tab`, '') <> IFNULL(OLD.`show_overview_tab`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_overview_tab', OLD.`show_overview_tab`, NEW.`show_overview_tab`);
    END IF;
	
    IF IFNULL(NEW.`save_title_as_seo`, '') <> IFNULL(OLD.`save_title_as_seo`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'save_title_as_seo', OLD.`save_title_as_seo`, NEW.`save_title_as_seo`);
    END IF;
	
    IF IFNULL(NEW.`api_after_insert`, '') <> IFNULL(OLD.`api_after_insert`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_after_insert', OLD.`api_after_insert`, NEW.`api_after_insert`);
    END IF;
	
    IF IFNULL(NEW.`api_after_update`, '') <> IFNULL(OLD.`api_after_update`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_after_update', OLD.`api_after_update`, NEW.`api_after_update`);
    END IF;
	
    IF IFNULL(NEW.`api_before_update`, '') <> IFNULL(OLD.`api_before_update`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_before_update', OLD.`api_before_update`, NEW.`api_before_update`);
    END IF;
	
    IF IFNULL(NEW.`api_before_delete`, '') <> IFNULL(OLD.`api_before_delete`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_before_delete', OLD.`api_before_delete`, NEW.`api_before_delete`);
    END IF;
	
    IF IFNULL(NEW.`show_title_field`, '') <> IFNULL(OLD.`show_title_field`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_title_field', OLD.`show_title_field`, NEW.`show_title_field`);
    END IF;
	
    IF IFNULL(NEW.`friendly_name`, '') <> IFNULL(OLD.`friendly_name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'friendly_name', OLD.`friendly_name`, NEW.`friendly_name`);
    END IF;
	
    IF IFNULL(NEW.`default_ordering`, '') <> IFNULL(OLD.`default_ordering`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'default_ordering', OLD.`default_ordering`, NEW.`default_ordering`);
    END IF;
	
    IF IFNULL(NEW.`save_history`, '') <> IFNULL(OLD.`save_history`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'save_history', OLD.`save_history`, NEW.`save_history`);
    END IF;
	
    IF IFNULL(NEW.`enable_multiple_environments`, '') <> IFNULL(OLD.`enable_multiple_environments`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'enable_multiple_environments', OLD.`enable_multiple_environments`, NEW.`enable_multiple_environments`);
    END IF;

    IF IFNULL(NEW.`use_dedicated_table`, '') <> IFNULL(OLD.`use_dedicated_table`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'use_dedicated_table', NULL, NEW.`use_dedicated_table`);
    END IF;
	
    IF IFNULL(NEW.`dedicated_table_prefix`, '') <> IFNULL(OLD.`dedicated_table_prefix`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'dedicated_table_prefix', OLD.`dedicated_table_prefix`, NEW.`dedicated_table_prefix`);
    END IF;
END;

DROP TRIGGER IF EXISTS `EntityDelete`;
CREATE TRIGGER `EntityDelete` AFTER DELETE ON `wiser_entity` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_ENTITY', 'wiser_entity', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_field_templates
-- ----------------------------
DROP TRIGGER IF EXISTS `FieldTemplateInsert`;
CREATE TRIGGER `FieldTemplateInsert` AFTER INSERT ON `wiser_field_templates` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`field_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'field_type', NULL, NEW.`field_type`);
    END IF;

    IF IFNULL(NEW.`html_template`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'html_template', NULL, NEW.`html_template`);
    END IF;

    IF IFNULL(NEW.`script_template`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'script_template', NULL, NEW.`script_template`);
    END IF;
END;

DROP TRIGGER IF EXISTS `FieldTemplateUpdate`;
CREATE TRIGGER `FieldTemplateUpdate` AFTER UPDATE ON `wiser_field_templates` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`field_type`, '') <> IFNULL(OLD.`field_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'field_type', OLD.`field_type`, NEW.`field_type`);
    END IF;
	
    IF IFNULL(NEW.`field_type`, '') <> IFNULL(OLD.`field_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'field_type', OLD.`field_type`, NEW.`field_type`);
    END IF;
	
    IF IFNULL(NEW.`script_template`, '') <> IFNULL(OLD.`script_template`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'script_template', OLD.`script_template`, NEW.`script_template`);
    END IF;
END;

DROP TRIGGER IF EXISTS `FieldTemplateDelete`;
CREATE TRIGGER `FieldTemplateDelete` AFTER DELETE ON `wiser_field_templates` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_FIELD_TEMPLATE', 'wiser_field_templates', OLD.id, IFNULL(@_username, USER()), 'field_type', OLD.field_type, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_link
-- ----------------------------
DROP TRIGGER IF EXISTS `LinkSettingInsert`;
CREATE TRIGGER `LinkSettingInsert` AFTER INSERT ON `wiser_link` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);
	
    IF IFNULL(NEW.`type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'type', NULL, NEW.`type`);
    END IF;
	
    IF IFNULL(NEW.`destination_entity_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'destination_entity_type', NULL, NEW.`destination_entity_type`);
    END IF;
	
    IF IFNULL(NEW.`connected_entity_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'connected_entity_type', NULL, NEW.`connected_entity_type`);
    END IF;
	
    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;
	
    IF IFNULL(NEW.`show_in_tree_view`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', NULL, NEW.`show_in_tree_view`);
    END IF;
	
    IF IFNULL(NEW.`show_in_data_selector`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_data_selector', NULL, NEW.`show_in_data_selector`);
    END IF;
	
    IF IFNULL(NEW.`relationship`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'relationship', NULL, NEW.`relationship`);
    END IF;
	
    IF IFNULL(NEW.`duplication`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'duplication', NULL, NEW.`duplication`);
    END IF;
	
    IF IFNULL(NEW.`show_in_tree_view`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', NULL, NEW.`show_in_tree_view`);
    END IF;
	
    IF IFNULL(NEW.`use_item_parent_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'use_item_parent_id', NULL, NEW.`use_item_parent_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkSettingUpdate`;
CREATE TRIGGER `LinkSettingUpdate` AFTER UPDATE ON `wiser_link` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`type`, '') <> IFNULL(OLD.`type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'type', OLD.`type`, NEW.`type`);
    END IF;
	
    IF IFNULL(NEW.`destination_entity_type`, '') <> IFNULL(OLD.`destination_entity_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'destination_entity_type', OLD.`destination_entity_type`, NEW.`destination_entity_type`);
    END IF;
	
    IF IFNULL(NEW.`connected_entity_type`, '') <> IFNULL(OLD.`connected_entity_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'connected_entity_type', OLD.`connected_entity_type`, NEW.`connected_entity_type`);
    END IF;
	
    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;
	
    IF IFNULL(NEW.`show_in_tree_view`, '') <> IFNULL(OLD.`show_in_tree_view`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', OLD.`show_in_tree_view`, NEW.`show_in_tree_view`);
    END IF;
	
    IF IFNULL(NEW.`show_in_data_selector`, '') <> IFNULL(OLD.`show_in_data_selector`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_data_selector', OLD.`show_in_data_selector`, NEW.`show_in_data_selector`);
    END IF;
	
    IF IFNULL(NEW.`relationship`, '') <> IFNULL(OLD.`relationship`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'relationship', OLD.`relationship`, NEW.`relationship`);
    END IF;
	
    IF IFNULL(NEW.`duplication`, '') <> IFNULL(OLD.`duplication`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'duplication', OLD.`duplication`, NEW.`duplication`);
    END IF;
	
    IF IFNULL(NEW.`show_in_tree_view`, '') <> IFNULL(OLD.`show_in_tree_view`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', OLD.`show_in_tree_view`, NEW.`show_in_tree_view`);
    END IF;
	
    IF IFNULL(NEW.`use_item_parent_id`, '') <> IFNULL(OLD.`use_item_parent_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'use_item_parent_id', OLD.`use_item_parent_id`, NEW.`use_item_parent_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkSettingDelete`;
CREATE TRIGGER `LinkSettingDelete` AFTER DELETE ON `wiser_link` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_LINK_SETTING', 'wiser_link', OLD.id, IFNULL(@_username, USER()), 'type/destination_entity_type/connected_entity_type', CONCAT_WS('/', OLD.type, OLD.destination_entity_type, OLD.connected_entity_type), '');
END;

-- ----------------------------
-- Triggers structure for table wiser_permission
-- ----------------------------
DROP TRIGGER IF EXISTS `PermissionInsert`;
CREATE TRIGGER `PermissionInsert` AFTER INSERT ON `wiser_permission` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`role_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'role_id', NULL, NEW.`role_id`);
    END IF;

    IF IFNULL(NEW.`entity_name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'entity_name', NULL, NEW.`entity_name`);
    END IF;

    IF IFNULL(NEW.`item_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'item_id', NULL, NEW.`item_id`);
    END IF;

    IF IFNULL(NEW.`entity_property_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'entity_property_id', NULL, NEW.`entity_property_id`);
    END IF;

    IF IFNULL(NEW.`permissions`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'permissions', NULL, NEW.`permissions`);
    END IF;

    IF IFNULL(NEW.`module_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'module_id', NULL, NEW.`module_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `PermissionUpdate`;
CREATE TRIGGER `PermissionUpdate` AFTER UPDATE ON `wiser_permission` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`role_id`, '') <> IFNULL(OLD.`role_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'role_id', OLD.`role_id`, NEW.`role_id`);
    END IF;
	
    IF IFNULL(NEW.`entity_name`, '') <> IFNULL(OLD.`entity_name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'entity_name', OLD.`entity_name`, NEW.`entity_name`);
    END IF;
	
    IF IFNULL(NEW.`item_id`, '') <> IFNULL(OLD.`item_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'item_id', OLD.`item_id`, NEW.`item_id`);
    END IF;
	
    IF IFNULL(NEW.`entity_property_id`, '') <> IFNULL(OLD.`entity_property_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'entity_property_id', OLD.`entity_property_id`, NEW.`entity_property_id`);
    END IF;
	
    IF IFNULL(NEW.`permissions`, '') <> IFNULL(OLD.`permissions`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'permissions', OLD.`permissions`, NEW.`permissions`);
    END IF;
	
    IF IFNULL(NEW.`module_id`, '') <> IFNULL(OLD.`module_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'module_id', OLD.`module_id`, NEW.`module_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `PermissionDelete`;
CREATE TRIGGER `PermissionDelete` AFTER DELETE ON `wiser_permission` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_PERMISSION', 'wiser_field_templates', OLD.id, IFNULL(@_username, USER()), 'old_data', JSON_OBJECT('role_id', OLD.role_id, 'entity_name', OLD.entity_name, 'item_id', OLD.item_id, 'entity_property_id', OLD.entity_property_id, 'permissions', OLD.permissions, 'module_id', OLD.module_id), '');
END;

-- ----------------------------
-- Triggers structure for table wiser_user_roles
-- ----------------------------
DROP TRIGGER IF EXISTS `UserRoleInsert`;
CREATE TRIGGER `UserRoleInsert` AFTER INSERT ON `wiser_user_roles` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`user_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'user_id', NULL, NEW.`user_id`);
    END IF;

    IF IFNULL(NEW.`role_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'role_id', NULL, NEW.`role_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `UserRoleUpdate`;
CREATE TRIGGER `UserRoleUpdate` AFTER UPDATE ON `wiser_user_roles` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`user_id`, '') <> IFNULL(OLD.`user_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'user_id', OLD.`user_id`, NEW.`user_id`);
    END IF;
	
    IF IFNULL(NEW.`role_id`, '') <> IFNULL(OLD.`role_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'role_id', OLD.`role_id`, NEW.`role_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `UserRoleDelete`;
CREATE TRIGGER `UserRoleDelete` AFTER DELETE ON `wiser_user_roles` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_USER_ROLE', 'wiser_user_roles', OLD.id, IFNULL(@_username, USER()), 'old_data', JSON_OBJECT('user_id', OLD.user_id, 'role_id', OLD.role_id), '');
END;

-- ----------------------------
-- Triggers structure for table wiser_api_connection
-- ----------------------------
DROP TRIGGER IF EXISTS `ApiConnectionInsert`;
CREATE TRIGGER `ApiConnectionInsert` AFTER INSERT ON `wiser_api_connection` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`options`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'options', NULL, NEW.`options`);
    END IF;

    IF IFNULL(NEW.`authentication_data`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'authentication_data', NULL, NEW.`authentication_data`);
    END IF;
END;

DROP TRIGGER IF EXISTS `ApiConnectionUpdate`;
CREATE TRIGGER `ApiConnectionUpdate` AFTER UPDATE ON `wiser_api_connection` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;
	
    IF IFNULL(NEW.`options`, '') <> IFNULL(OLD.`options`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'options', OLD.`options`, NEW.`options`);
    END IF;
	
    IF IFNULL(NEW.`authentication_data`, '') <> IFNULL(OLD.`authentication_data`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'authentication_data', OLD.`authentication_data`, NEW.`authentication_data`);
    END IF;
END;

DROP TRIGGER IF EXISTS `ApiConnectionDelete`;
CREATE TRIGGER `ApiConnectionDelete` AFTER DELETE ON `wiser_api_connection` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_API_CONNECTION', 'wiser_api_connection', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_data_selector
-- ----------------------------
DROP TRIGGER IF EXISTS `DataSelectorInsert`;
CREATE TRIGGER `DataSelectorInsert` AFTER INSERT ON `wiser_data_selector` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`removed`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'removed', NULL, NEW.`removed`);
    END IF;

    IF IFNULL(NEW.`module_selection`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'module_selection', NULL, NEW.`module_selection`);
    END IF;

    IF IFNULL(NEW.`request_json`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'request_json', NULL, NEW.`request_json`);
    END IF;

    IF IFNULL(NEW.`saved_json`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'saved_json', NULL, NEW.`saved_json`);
    END IF;

    IF IFNULL(NEW.`show_in_export_module`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'show_in_export_module', NULL, NEW.`show_in_export_module`);
    END IF;
END;

DROP TRIGGER IF EXISTS `DataSelectorUpdate`;
CREATE TRIGGER `DataSelectorUpdate` AFTER UPDATE ON `wiser_data_selector` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;
	
    IF IFNULL(NEW.`removed`, '') <> IFNULL(OLD.`removed`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'removed', OLD.`removed`, NEW.`removed`);
    END IF;
	
    IF IFNULL(NEW.`module_selection`, '') <> IFNULL(OLD.`module_selection`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'module_selection', OLD.`module_selection`, NEW.`module_selection`);
    END IF;
	
    IF IFNULL(NEW.`request_json`, '') <> IFNULL(OLD.`request_json`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'request_json', OLD.`request_json`, NEW.`request_json`);
    END IF;
	
    IF IFNULL(NEW.`saved_json`, '') <> IFNULL(OLD.`saved_json`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'saved_json', OLD.`saved_json`, NEW.`saved_json`);
    END IF;
	
    IF IFNULL(NEW.`show_in_export_module`, '') <> IFNULL(OLD.`show_in_export_module`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'show_in_export_module', OLD.`show_in_export_module`, NEW.`show_in_export_module`);
    END IF;
END;

DROP TRIGGER IF EXISTS `DataSelectorDelete`;
CREATE TRIGGER `DataSelectorDelete` AFTER DELETE ON `wiser_data_selector` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_DATA_SELECTOR', 'wiser_data_selector', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;