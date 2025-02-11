-------------------------------------------------------------------


-- dbo, add all non-PK constraints
ALTER TABLE dbo.RAppsRoot ADD CONSTRAINT [FK_RAppsRoot_VFolders_RootFolderID] FOREIGN KEY (RootFolderID) REFERENCES dbo.VFolders(ID)
ALTER TABLE dbo.RAppsRoot ADD CONSTRAINT FK_RAppsRoot_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.RAppsRoot ADD CONSTRAINT FK_RAppsRoot_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VUsers ADD CONSTRAINT [FK_VUsers_VRoles_RoleId] FOREIGN KEY (RoleID) REFERENCES dbo.VRoles(ID)
ALTER TABLE [dbo].VUsers ADD CONSTRAINT FK_VUsers_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE	[dbo].VUsers ADD CONSTRAINT FK_VUsers_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE [dbo].[VRoles] ADD CONSTRAINT FK_VRoles_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE	[dbo].[VRoles] ADD CONSTRAINT FK_VRoles_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VSystems ADD CONSTRAINT FK_VSystems_VFolders_RootFolderID FOREIGN KEY (RootFolderID) REFERENCES dbo.VFolders(ID)
ALTER TABLE dbo.VSystems ADD CONSTRAINT FK_VSystems_VUsers_AssignedTo FOREIGN KEY (AssignedTo) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VSystems ADD CONSTRAINT FK_VSystems_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VSystems ADD CONSTRAINT FK_VSystems_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VApps ADD CONSTRAINT FK_VApps_VUsers_Owner FOREIGN KEY (Owner) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VApps ADD CONSTRAINT FK_VApps_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VApps ADD CONSTRAINT FK_VApps_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.VFolders ADD CONSTRAINT FK_VFolders_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VFolders ADD CONSTRAINT FK_VFolders_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VFolders ADD CONSTRAINT UC_Path UNIQUE (Path)

ALTER TABLE dbo.VFiles ADD CONSTRAINT FK_VFiles_FileTypes_FileTypeID FOREIGN KEY (FileTypeID) REFERENCES dbo.FileTypes(ID) 
ALTER TABLE dbo.VFiles ADD CONSTRAINT FK_VFiles_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.VFiles ADD CONSTRAINT FK_VFiles_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.FileTypes ADD CONSTRAINT FK_FileTypes_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.FileTypes ADD CONSTRAINT FK_FileTypes_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

-- dbo join tables constraints
ALTER TABLE dbo.SystemUsers ADD CONSTRAINT FK_SystemUsers_VSystems_VSystemID FOREIGN KEY (VSystemID) REFERENCES dbo.VSystems(ID)
ALTER TABLE dbo.SystemUsers ADD CONSTRAINT FK_SystemUsers_VUsers_VUserID FOREIGN KEY (VUserID) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemUsers ADD CONSTRAINT FK_SystemUsers_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemUsers ADD CONSTRAINT FK_SystemUsers_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VSystems_VSystemID FOREIGN KEY (VSystemID) REFERENCES dbo.VSystems(ID)
ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VUsers_VUserID FOREIGN KEY (VUserID) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VApps_VAppID FOREIGN KEY (VAppID) REFERENCES dbo.VApps(ID)
ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemUserApps ADD CONSTRAINT FK_SystemUserApps_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VSystems_VSystemID FOREIGN KEY (VSystemID) REFERENCES dbo.VSystems(ID)
ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VFolders_VFolderID FOREIGN KEY (VFolderID) REFERENCES dbo.VFolders(ID)
ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VUsers_CheckedOutBy FOREIGN KEY (CheckedOutBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.SystemFolderFiles ADD CONSTRAINT FK_SystemFolderFiles_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE dbo.FileTypeApps ADD CONSTRAINT FK_FileTypeApps_FileTypes_VFileTypeID FOREIGN KEY (VFileTypeID) REFERENCES dbo.FileTypes(ID)
ALTER TABLE dbo.FileTypeApps ADD CONSTRAINT FK_FileTypeApps_VApps_VAppID FOREIGN KEY (VAppID) REFERENCES dbo.VApps(ID)
ALTER TABLE dbo.FileTypeApps ADD CONSTRAINT FK_FileTypeApps_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE dbo.FileTypeApps ADD CONSTRAINT FK_FileTypeApps_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)
