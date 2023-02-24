CREATE DEFINER=CURRENT_USER FUNCTION `CreateJsonSafeProperty`(input VARCHAR(255)) RETURNS text CHARSET utf8mb4 DETERMINISTIC
BEGIN
    DECLARE output VARCHAR(255);

    SET output = REPLACE(input, '-', '__h__');
    SET output = REPLACE(output, ' ', '__s__');
    SET output = REPLACE(output, ':', '__c__');
    SET output = REPLACE(output, '(', '__bl__');
    SET output = REPLACE(output, ')', '__br__');
    SET output = REPLACE(output, '.', '__d__');
    SET output = REPLACE(output, ',', '__co__');

    RETURN output;
END;

CREATE DEFINER=CURRENT_USER FUNCTION `GetNameFromSafeJsonProperty`(input VARCHAR(255)) RETURNS text CHARSET utf8mb4 DETERMINISTIC
BEGIN
    DECLARE output VARCHAR(255);

    SET output = REPLACE(input, '__h__', '-');
    SET output = REPLACE(output, '__s__', ' ');
    SET output = REPLACE(output, '__c__', ':');
    SET output = REPLACE(output, '__bl__', '(');
    SET output = REPLACE(output, '__br__', ')');
    SET output = REPLACE(output, '__d__', '.');
    SET output = REPLACE(output, '__co__', ',');

    RETURN output;
END;

CREATE DEFINER=CURRENT_USER PROCEDURE `DeleteWiser2Item`(`id` BIGINT, `entityType` VARCHAR(100))
BEGIN
    DECLARE tablePrefix VARCHAR(50) DEFAULT '';

    IF entityType IS NOT NULL AND entityType != '' THEN
        SELECT IFNULL(dedicated_table_prefix, '')
        INTO tablePrefix
        FROM wiser_entity
        WHERE name = entityType;

        IF tablePrefix IS NOT NULL AND tablePrefix != '' AND tablePrefix NOT LIKE '%\_' THEN
            SET tablePrefix = CONCAT(tablePrefix, '_');
        END IF;
    END IF;

    # Copy the items themselves to the archive.
    SET @copyItemQuery = REPLACE(REPLACE('
		INSERT IGNORE INTO {tablePrefix}wiser_item_archive
		(
			id, 
			original_item_id, 
			parent_item_id, 
			unique_uuid, 
			entity_type, 
			moduleid, 
			published_environment, 
			readonly, 
			title, 
			added_on, 
			added_by, 
			changed_on, 
			changed_by
		)
		SELECT
			id, 
			original_item_id, 
			parent_item_id, 
			unique_uuid, 
			entity_type, 
			moduleid, 
			published_environment, 
			readonly, 
			title, 
			added_on, 
			added_by, 
			changed_on, 
			changed_by
		FROM {tablePrefix}wiser_item
		WHERE id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);

    # Copy the item details to the arhive.
    SET @copyDetailsQuery = REPLACE(REPLACE('
		INSERT IGNORE INTO {tablePrefix}wiser_itemdetail_archive
		(
			id,
			language_code,
			item_id,
			groupname,
			`key`,
			value,
			long_value
		)
		SELECT
			detail.id,
			detail.language_code,
			detail.item_id,
			detail.groupname,
			detail.`key`,
			detail.value,
			detail.long_value
		FROM {tablePrefix}wiser_itemdetail AS detail
		WHERE detail.item_id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);

    # Copy the item files to the archive.
    SET @copyFilesQuery = REPLACE(REPLACE('
		INSERT IGNORE INTO wiser_itemfile_archive
		(
			id,
			item_id,
			content_type,
			content,
			content_url,
			width,
			height,
			file_name,
			extension,
			added_on,
			added_by,
			title,
			property_name,
			itemlink_id
		)
		SELECT
			file.id,
			file.item_id,
			file.content_type,
			file.content,
			file.content_url,
			file.width,
			file.height,
			file.file_name,
			file.extension,
			file.added_on,
			file.added_by,
			file.title,
			file.property_name,
			file.itemlink_id
		FROM wiser_itemfile AS file
		WHERE file.item_id = {id}
		
		UNION ALL
		
		SELECT
			file.id,
			file.item_id,
			file.content_type,
			file.content,
			file.content_url,
			file.width,
			file.height,
			file.file_name,
			file.extension,
			file.added_on,
			file.added_by,
			file.title,
			file.property_name,
			file.itemlink_id
		FROM wiser_itemlink AS link
		JOIN wiser_itemfile AS file ON file.itemlink_id = link.id
		WHERE (link.item_id = {id} OR link.destination_item_id = {id})', '{tablePrefix}', tablePrefix), '{id}', id);

    # Copy the item links to the archive.
    SET @copyLinksQuery = REPLACE(REPLACE('
		INSERT IGNORE INTO wiser_itemlink_archive
		(
			id,
			item_id,
			destination_item_id,
			ordering,
			type,
			added_on
		)
		SELECT
			link.id,
			link.item_id,
			link.destination_item_id,
			link.ordering,
			link.type,
			link.added_on
		FROM wiser_itemlink AS link
		WHERE item_id = {id} OR destination_item_id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);

    # Copy the item link details to the archive.
    SET @copyLinkDetailsQuery = REPLACE(REPLACE('
		INSERT IGNORE INTO wiser_itemlinkdetail_archive
		(
			id,
			language_code,
			itemlink_id,
			groupname,
			`key`,
			value,
			long_value
		)
		SELECT
			detail.id,
			detail.language_code,
			detail.itemlink_id,
			detail.groupname,
			detail.`key`,
			detail.value,
			detail.long_value
		FROM wiser_item AS item
		JOIN wiser_itemlink AS link ON (link.item_id = item.id OR link.destination_item_id = item.id)
		JOIN wiser_itemlinkdetail AS detail ON detail.itemlink_id = link.id
		WHERE item.id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);

    # And finally delete the removed items from the original tables.
    SET @deleteLinkDetailsQuery = REPLACE(REPLACE('DELETE detail.* FROM {tablePrefix}wiser_item AS item JOIN wiser_itemlink AS link ON link.item_id = item.id OR link.destination_item_id = item.id JOIN wiser_itemlinkdetail AS detail ON detail.itemlink_id = link.id WHERE item.id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);
    SET @deleteLinksQuery = REPLACE(REPLACE('DELETE FROM wiser_itemlink WHERE item_id = {id} OR destination_item_id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);
    SET @deleteDetailsQuery = REPLACE(REPLACE('DELETE FROM {tablePrefix}wiser_itemdetail WHERE item_id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);
    SET @deleteFilesQuery = REPLACE(REPLACE('DELETE FROM wiser_itemfile WHERE item_id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);
    SET @deleteItemQuery = REPLACE(REPLACE('DELETE FROM {tablePrefix}wiser_item WHERE id = {id}', '{tablePrefix}', tablePrefix), '{id}', id);

    PREPARE statement1 FROM @copyItemQuery;
    EXECUTE statement1;

    PREPARE statement2 FROM @copyDetailsQuery;
    EXECUTE statement2;

    PREPARE statement3 FROM @copyFilesQuery;
    EXECUTE statement3;

    PREPARE statement4 FROM @copyLinksQuery;
    EXECUTE statement4;

    PREPARE statement5 FROM @copyLinkDetailsQuery;
    EXECUTE statement5;

    PREPARE statement6 FROM @deleteLinkDetailsQuery;
    EXECUTE statement6;

    PREPARE statement7 FROM @deleteLinksQuery;
    EXECUTE statement7;

    PREPARE statement8 FROM @deleteDetailsQuery;
    EXECUTE statement8;

    PREPARE statement9 FROM @deleteFilesQuery;
    EXECUTE statement9;

    PREPARE statement10 FROM @deleteItemQuery;
    EXECUTE statement10;
END;