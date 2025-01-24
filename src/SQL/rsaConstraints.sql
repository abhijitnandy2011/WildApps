-----------------------------------------------------------
-- rsa, add constraints
ALTER TABLE rsa.Workbooks ADD CONSTRAINT FK_RSA_Workbooks_VFiles_VFileID FOREIGN KEY (VFileID) REFERENCES dbo.VFiles(ID)
ALTER TABLE rsa.Workbooks ADD CONSTRAINT FK_RSA_Workbooks_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE rsa.Workbooks ADD CONSTRAINT FK_RSA_Workbooks_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE rsa.Sheets ADD CONSTRAINT FK_RSA_Sheets_Workbooks_WorkbookID FOREIGN KEY (WorkbookID) REFERENCES rsa.Workbooks(ID)
ALTER TABLE rsa.Sheets ADD CONSTRAINT FK_RSA_Sheets_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE rsa.Sheets ADD CONSTRAINT FK_RSA_Sheets_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE rsa.XlTables ADD CONSTRAINT FK_RSA_XlTables_Sheets_SheetID FOREIGN KEY (SheetID) REFERENCES rsa.Sheets(ID)
ALTER TABLE rsa.XlTables ADD CONSTRAINT FK_RSA_XlTables_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE rsa.XlTables ADD CONSTRAINT FK_RSA_XlTables_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)

ALTER TABLE rsa.Cells ADD CONSTRAINT FK_RSA_Cells_Sheets_SheetID FOREIGN KEY (SheetID) REFERENCES rsa.Sheets(ID)
ALTER TABLE rsa.Cells ADD CONSTRAINT FK_RSA_Cells_VUsers_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.VUsers(ID)
ALTER TABLE rsa.Cells ADD CONSTRAINT FK_RSA_Cells_VUsers_LastUpdatedBy FOREIGN KEY (LastUpdatedBy) REFERENCES dbo.VUsers(ID)
