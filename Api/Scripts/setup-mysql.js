async function main() {
    const util = require("util");
    const arguments = require("minimist")(process.argv.slice(2));
    const clc = require("cli-color");
    const error = clc.red.bold;
    const warn = clc.yellow;
    const notice = clc.blue;
    const mysql = require("mysql");
    const path = require("path");
    const fs = require("fs");

    function createConnection(config) {
        const connection = mysql.createConnection(config);
        return {
            query(sql, args) {
                return util.promisify(connection.query).call(connection, sql, args);
            },
            changeUser(args) {
                return util.promisify(connection.changeUser).call(connection, args);
            },
            close() {
                return util.promisify(connection.end).call(connection);
            }
        };
    }

    if (!arguments.host || !arguments.database || !arguments.user || !arguments.password) {
        console.error(error("Please supply a MySQL host, database, user and password."));
        return;
    }

    const connection = await createConnection({
        host: arguments.host,
        user: arguments.user,
        password: arguments.password,
        port: arguments.port || 3306,
        multipleStatements: true,
        charset: "utf8mb4_general_ci"
    });

    try {
        console.log(notice(`Creating database "${arguments.database}"...`));
        await connection.query(`CREATE DATABASE \`${arguments.database}\`;`);
        console.log(notice(`Database created.`));

        console.log(notice(`Connecting to database "${arguments.database}"...`));
        await connection.changeUser({ database: arguments.database });
        console.log(notice(`Connected to database.`));

        console.log(notice("Creating tables..."));
        await connection.query(`CREATE TABLE \`easy_customers\`  (
                              \`id\` int NOT NULL AUTO_INCREMENT,
                              \`customerid\` int NULL DEFAULT NULL,
                              \`name\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`db_host\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`db_login\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`db_pass\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`db_passencrypted\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NULL DEFAULT NULL,
                              \`db_port\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`db_dbname\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`encryption_key\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`encryption_key_test\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`subdomain\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              \`wiser_title\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
                              PRIMARY KEY (\`id\`) USING BTREE,
                              UNIQUE INDEX \`subdomain\`(\`subdomain\`) USING BTREE,
                              INDEX \`customerid\`(\`customerid\`) USING BTREE,
                              INDEX \`name\`(\`name\`) USING BTREE
                            ) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;`);
        const createTablesQuery = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/CreateTables.sql"), "utf8");
        await connection.query(createTablesQuery);
        console.log(notice("Tables created."));

        console.log(notice("Creating triggers..."));
        const createTriggersQuery = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/CreateTriggers.sql"), "utf8");
        await connection.query(createTriggersQuery);
        console.log(notice("Triggers created."));
		
		console.log(notice("Creating stored procedures..."));
		const createStoredProceduresQuery = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/StoredProcedures.sql"), "utf8");
		await connection.query(createStoredProceduresQuery);
		console.log(notice("Stored procedures created."));

        console.log(notice("Inserting data..."));
        await connection.query(`INSERT INTO easy_customers (id, customerid, name, subdomain, wiser_title) VALUES (1, 1, 'Main', 'main', 'Wiser')`);
        const insertInitialDataQuery = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/InsertInitialData.sql"), "utf8");
        await connection.query(insertInitialDataQuery.replace("?newCustomerId", "1"));
        console.log(notice("Data inserted."));

        const insertInitialWTSDataQuery = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/InsertInitialWTSData.sql"), "utf8");
        await connection.query(insertInitialWTSDataQuery
                .replace("?setting_hostname", arguments.host)
                .replace("?setting_port", arguments.port)
                .replace("?setting_username", arguments.user)
                .replace("?setting_password", arguments.password)
                .replace("?setting_database", arguments.database)
        );
        console.log(notice("Data inserted."));

        if (arguments.isMultiLanguage) {
            console.log(notice("Setting up multi language support..."));
            const insertInitialDataMultiLanguage = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/InsertInitialDataMultiLanguage.sql"), "utf8");
            connection.query(insertInitialDataMultiLanguage);
            console.log(notice("Multi language support installed."));
        }

        if (arguments.isConfigurator) {
            console.log(notice("Creating configurator tables..."));
            const createTablesConfigurator = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/CreateTablesConfigurator.sql"), "utf8");
            await connection.query(createTablesConfigurator);
            console.log(notice("Tables created."));

            console.log(notice("Setting up configurator..."));
            const insertInitialDataConfiguratorQuery = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/InsertInitialDataConfigurator.sql"), "utf8");
            await connection.query(insertInitialDataConfiguratorQuery);
            console.log(notice("Configurator setup."));
        }

        if (arguments.isWebshop) {
            console.log(notice("Setting up webshop..."));
            const insertInitialDataEcommerceQuery = fs.readFileSync(path.join(__dirname, "..", "/Core/Queries/WiserInstallation/InsertInitialDataEcommerce.sql"), "utf8");
            connection.query(insertInitialDataEcommerceQuery);
            console.log(notice("Webshop setup."));
        }
    } finally {
        await connection.close();
    }
}

main();