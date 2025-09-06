CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `HitsterGameSet` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Sku` longtext NOT NULL,
    `Language` longtext NOT NULL,
    `SetName` longtext NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `HitsterCard` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CardNumber` longtext NOT NULL,
    `Spotify` longtext NOT NULL,
    `HitsterGameSetId` int NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_HitsterCard_HitsterGameSet_HitsterGameSetId` FOREIGN KEY (`HitsterGameSetId`) REFERENCES `HitsterGameSet` (`Id`)
);

CREATE INDEX `IX_HitsterCard_HitsterGameSetId` ON `HitsterCard` (`HitsterGameSetId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250626195035_InitialCreate', '9.0.6');

COMMIT;

