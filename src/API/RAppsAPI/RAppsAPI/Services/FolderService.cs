using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RAppsAPI.Data;
using RAppsAPI.Models;
using System.Collections.Generic;
using static RAppsAPI.Data.DBConstants;

namespace RAppsAPI.Services
{
    public class FolderService(RDBContext dbContext) : IFolderService
    {
        public async Task<FolderObjectUpdateResponseDTO> Create(string folderName, string attrs, string parentPath, int createdByUserID)
        {
            throw new NotImplementedException();
        }

        public async Task<FolderObjectUpdateResponseDTO> Create(string folderName, string attrs, int parentFolderID, int createdByUserID)
        {
            using var transaction = dbContext.Database.BeginTransaction();
            try
            {
                var respObj = new FolderObjectUpdateResponseDTO();
                respObj.Id = -1;
                respObj.ObjectType = (int)FolderObjectType.Folder;
                var parentFolder = await dbContext.VFolders.Where<VFolder>(f => f.Id == parentFolderID).FirstOrDefaultAsync();
                if (parentFolder != null)
                {
                    // Transactions are used by SaveChanges()
                    // But we will need to do a SaveChanges() to get the new folder's ID & then again to map it.
                    var newPath = string.Join(DBConstants.PathSep, [parentFolder.Path, folderName]);
                    var newParentIDs = string.Join(DBConstants.PathIDSep, [parentFolder.ParentIds, parentFolderID]);
                    var newVFolder = new VFolder()
                    {
                        Name = folderName,
                        Attrs = attrs,
                        Path = newPath,
                        ParentIds = newParentIDs,
                        CreatedBy = createdByUserID,
                        CreatedDate = DateTime.Now,
                        Rstatus = (int)DBConstants.RStatus.Active

                    };
                    // Create folder in DB
                    await dbContext.VFolders.AddAsync(newVFolder);
                    await dbContext.SaveChangesAsync();
                    // Add Mapping
                    var newFolderID = newVFolder.Id;
                    var newSystemFolderFile = new SystemFolderFile()
                    {
                        VSystemId = DBConstants.USER_SYSTEM_ID,
                        VFolderId = newFolderID,
                        VParentFolderId = parentFolderID,
                        CreatedBy = createdByUserID,
                        CreatedDate = DateTime.Now,
                        Rstatus = (int)DBConstants.RStatus.Active

                    };
                    // Create mapping
                    await dbContext.SystemFolderFiles.AddAsync(newSystemFolderFile);
                    await dbContext.SaveChangesAsync();
                    // Commit
                    transaction.Commit();
                    // response
                    respObj.Code = (int)Constants.ResponseReturnCode.Success;
                    respObj.Message = "success";
                }
                else
                {
                    // error
                    respObj.Code = (int)Constants.ResponseReturnCode.Error;
                    respObj.Message = $"No folder with id {parentFolderID}";
                    transaction.Rollback();
                }

                return respObj;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // TODO: Log expception message/error                
                return new FolderObjectUpdateResponseDTO()
                {
                    Code = (int)Constants.ResponseReturnCode.Error,
                    Message = "Failed to create sub folder"
                };
            }
        }

        public Task<List<FolderObjectDTO>?> Read(string path)
        {
            throw new NotImplementedException();
        }

        public async Task<List<FolderObjectDTO>?> Read(int parentFolderID)
        {
            try
            {
                var dbObjList = await dbContext.SystemFolderFiles
                    .Include(sysff => sysff.File)
                    .Include(sysff => sysff.File.CreatedByUser)
                    .Include(sysff => sysff.Folder)
                    .Include(sysff => sysff.Folder.CreatedByUser)
                    .Include(sysff => sysff.ParentFolder)
                    .Where(r => r.VParentFolderId == parentFolderID && r.Rstatus == (byte)RStatus.Active)
                    .ToListAsync();
                if (dbObjList == null)
                {
                    return null;
                }
                // Make a list
                var objList = new List<FolderObjectDTO>();
                foreach (var dbSysFolderFile in dbObjList)
                {
                    var objId = dbSysFolderFile.VFolderId ?? dbSysFolderFile.VFileId;
                    if (objId == null)
                    {
                        // Log error
                    }
                    else 
                    {
                        var objName = dbSysFolderFile.Folder?.Name ?? dbSysFolderFile.File?.Name;
                        if (objName == null)
                        {
                            // Log error
                        }
                        else
                        {
                            FolderObjectType objType = dbSysFolderFile.VFolderId != null ? FolderObjectType.Folder :
                                 (dbSysFolderFile.VFileId != null ? FolderObjectType.File : FolderObjectType.Link);
                            string objAttrs = dbSysFolderFile.Folder?.Attrs ?? dbSysFolderFile.File?.Attrs ?? string.Empty;
                            string objCreatedByUser = dbSysFolderFile.Folder?.CreatedByUser.UserName ?? dbSysFolderFile.File?.CreatedByUser.UserName ?? string.Empty;
                            string objCreatedDate = dbSysFolderFile.Folder?.CreatedDate.ToString() ?? dbSysFolderFile.File?.CreatedDate.ToString() ?? string.Empty;
                            string objLastUpdatedByUser = dbSysFolderFile.Folder?.LastUpdatedByUser?.UserName ?? dbSysFolderFile.File?.LastUpdatedByUser?.UserName ?? string.Empty;
                            string objLastUpdatedDate = dbSysFolderFile.Folder?.LastUpdatedDate?.ToString() ?? dbSysFolderFile.File?.LastUpdatedDate?.ToString() ?? string.Empty;

                            // TODO: This should come from the DB where the app to URL mapping will be stored when the app 
                            // registers.
                            string openUrl = "";
                            switch(objType)
                            {
                                case FolderObjectType.Folder:
                                    openUrl = "/files/" + objId;
                                    break;
                                case FolderObjectType.File:
                                    switch(dbSysFolderFile.File?.FileTypeId)
                                    {
                                        case 1:
                                            openUrl = "/notepad/" + objId;
                                            break;
                                        case 2:
                                            openUrl = "/rsheet/" + objId;
                                            break;
                                            // TODO Other types
                                    }
                                    break;
                            }

                            objList.Add(new FolderObjectDTO
                            {
                                Id = (int)objId,
                                Name = (string)objName,
                                Description = "",   // Unused currently
                                Path = dbSysFolderFile.Folder?.Path ?? string.Empty,
                                ObjectType = (int)objType,
                                Attributes = objAttrs,
                                //IconURL = d,
                                OpenUrl = openUrl,
                                CreatedBy = objCreatedByUser,
                                CreatedDate = objCreatedDate,
                                LastUpdatedBy = objLastUpdatedByUser,
                                LastUpdatedDate = objLastUpdatedDate
                            });
                        }
                    }


                    

                }                
                return objList;
            }
            catch (Exception ex)
            {
                // TODO: Log error
                return new List<FolderObjectDTO>();
            }            
        }

        public async Task<FolderObjectUpdateResponseDTO> updateFolder(int folderID, string newName, string attrs, string modifiedByUserName)
        {
            try
            {
                return new FolderObjectUpdateResponseDTO();
            }
            catch (Exception ex)
            {
                // TODO: Log error
                return new FolderObjectUpdateResponseDTO();
            }
            
        }
    }
}
