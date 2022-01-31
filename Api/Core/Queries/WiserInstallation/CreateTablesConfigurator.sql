SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for configurations_wiser_item
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_item`  (
  `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `original_item_id` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `parent_item_id` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `ordering` mediumint(9) NOT NULL DEFAULT 0,
  `unique_uuid` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `entity_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `moduleid` mediumint(9) NOT NULL DEFAULT 0,
  `published_environment` mediumint(9) NOT NULL DEFAULT 15 COMMENT 'Bitwise, 0 = hidden, 1 = development, 2 = test, 4 = acceptance, 8 = live',
  `readonly` tinyint(4) NOT NULL DEFAULT 0,
  `title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `added_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `changed_on` datetime NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `removed` tinyint(4) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `moduleid`(`moduleid`, `published_environment`) USING BTREE,
  INDEX `entity_type`(`entity_type`, `unique_uuid`) USING BTREE,
  INDEX `unique_uuid`(`unique_uuid`) USING BTREE,
  INDEX `entity_environment`(`entity_type`, `published_environment`) USING BTREE,
  INDEX `original_item_id`(`original_item_id`) USING BTREE,
  INDEX `parent`(`parent_item_id`, `entity_type`, `removed`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 3863 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for configurations_wiser_item_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_item_archive`  (
  `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `original_item_id` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `parent_item_id` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `unique_uuid` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `entity_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `moduleid` mediumint(9) NOT NULL DEFAULT 0,
  `published_environment` mediumint(9) NOT NULL DEFAULT 15 COMMENT 'Bitwise, 0 = hidden, 1 = development, 2 = test, 4 = acceptance, 8 = live',
  `readonly` tinyint(4) NOT NULL DEFAULT 0,
  `title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `added_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `changed_on` datetime NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_moduleid`(`moduleid`, `published_environment`) USING BTREE,
  INDEX `idx_entity_type`(`entity_type`, `unique_uuid`) USING BTREE,
  INDEX `idx_unique_uuid`(`unique_uuid`) USING BTREE,
  INDEX `idx_entity_environment`(`entity_type`, `published_environment`) USING BTREE,
  INDEX `idx_original_item_id`(`original_item_id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for configurations_wiser_itemdetail
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemdetail`  (
  `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'leeg betekent beschikbaar voor alle talen',
  `item_id` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'optionele groepering van items, zoals een \'specs\' tabel',
  `key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `value` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `long_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor waardes die niet in \'value\' passen, zoals van HTMLeditors',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `item_key`(`item_id`, `key`, `language_code`) USING BTREE COMMENT 'voor opbouwen productoverzicht',
  INDEX `key_value`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items',
  INDEX `item_id_key_value`(`item_id`, `key`(40), `value`(40)) USING BTREE,
  INDEX `item_id_group`(`item_id`, `groupname`, `key`(40)) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 45332 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for configurations_wiser_itemdetail_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemdetail_archive`  (
  `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'leeg betekent beschikbaar voor alle talen',
  `item_id` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'optionele groepering van items, zoals een \'specs\' tabel',
  `key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `value` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `long_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor waardes die niet in \'value\' passen, zoals van HTMLeditors',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `item_key`(`item_id`, `key`, `language_code`) USING BTREE COMMENT 'voor opbouwen productoverzicht',
  UNIQUE INDEX `idx_main`(`item_id`, `key`, `language_code`) USING BTREE,
  INDEX `key_value`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items',
  INDEX `item_id_key_value`(`item_id`, `key`(40), `value`(40)) USING BTREE,
  INDEX `item_id_group`(`item_id`, `groupname`, `key`(40)) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 21542 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for configurations_wiser_itemlinkdetail
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemlinkdetail`  (
  `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'leeg betekent beschikbaar voor alle talen',
  `itemlink_id` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'optionele groepering van items, zoals een \'specs\' tabel',
  `key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `value` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `long_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor waardes die niet in \'value\' passen, zoals van HTMLeditors',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `itemlink_key`(`itemlink_id`, `key`, `language_code`) USING BTREE COMMENT 'voor opbouwen productoverzicht',
  INDEX `itemlink_id`(`itemlink_id`) USING BTREE COMMENT 'voor zoeken waardes van 1 item',
  INDEX `language_id`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items'
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for configurations_wiser_itemfile
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemfile`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `item_id` bigint(20) NOT NULL DEFAULT 0 COMMENT 'let op: dit is het item_id van de content',
  `content_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `content` longblob NULL,
  `content_url` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `width` smallint(6) NOT NULL DEFAULT 0,
  `height` smallint(6) NOT NULL DEFAULT 0,
  `file_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `extension` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `added_by` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `property_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL COMMENT 'De naam van het veld waar deze afbeelding bijhoort',
  `itemlink_id` bigint(20) NOT NULL DEFAULT 0,
  `protected` tinyint(255) NOT NULL DEFAULT 0 COMMENT 'Set to 1 to only allow the item file to be retrieved through an encrypted id.',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `item_id`(`item_id`, `content_type`) USING BTREE,
  INDEX `idx_itemlinkid`(`itemlink_id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

SET FOREIGN_KEY_CHECKS = 1;