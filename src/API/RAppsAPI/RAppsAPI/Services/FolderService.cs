using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RAppsAPI.Data;
using RAppsAPI.Models;
using RAppsAPI.utils;
using System.Collections.Generic;
using static RAppsAPI.Data.DBConstants;

namespace RAppsAPI.Services
{
    public class FolderService(RDBContext dbContext) : IFolderService
    {
        public async Task<FolderObjectUpdateResponseDTO> CreateUsingPath(string folderName, string attrs, string parentPath, int createdByUserID)
        {
            throw new NotImplementedException();
        }

        public async Task<FolderObjectUpdateResponseDTO> CreateUsingID(string folderName, string attrs, int parentFolderID, int createdByUserID)
        {
            try
            {
                int count = await dbContext.SystemFolderFiles
                    .CountAsync(sysff => sysff.VParentFolderId == parentFolderID &&
                                    sysff.Folder.Name == folderName &&
                                    sysff.Rstatus == (byte)RStatus.Active);
                if (count > 0)
                {
                    // Error! folder exists
                    return new FolderObjectUpdateResponseDTO()
                    {
                        Code = (int)Constants.ResponseReturnCode.Error,
                        Message = "Sub folder already exists"
                    };
                }
            }
            catch (Exception ex)
            {
                // TODO: Log exception message/error
                string exMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }
                return new FolderObjectUpdateResponseDTO()
                {
                    Code = (int)Constants.ResponseReturnCode.Error,
                    Message = "Failed to create sub folder:" + exMsg
                };
            }
            // Add the folder
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
                string exMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }
                return new FolderObjectUpdateResponseDTO()
                {
                    Code = (int)Constants.ResponseReturnCode.Error,
                    Message = "Failed to create sub folder:" + exMsg
                };
            }
        }

        public Task<FolderObjectReadResponseDTO> ReadUsingPath(string path)
        {
            throw new NotImplementedException();
        }

        public async Task<FolderObjectReadResponseDTO> ReadUsingID(int parentFolderID)
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
                    // No file/folder objects in this folder
                    return new FolderObjectReadResponseDTO()
                    {
                        Id = parentFolderID,
                        Code = (int)Constants.ResponseReturnCode.Success,
                        Message = "success",
                        FolderObjects = new List<FolderObjectDTO>()
                    };
                }
                // Make a list
                var objList = new List<FolderObjectDTO>();
                foreach (var dbSysFolderFile in dbObjList)
                {
                    var objId = dbSysFolderFile.VFolderId ?? dbSysFolderFile.VFileId;
                    if (objId == null)
                    {
                        // Log error & skip
                    }
                    else
                    {
                        var objName = dbSysFolderFile.Folder?.Name ?? dbSysFolderFile.File?.Name;
                        if (objName == null)
                        {
                            // Log error & skip
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
                            switch (objType)
                            {
                                case FolderObjectType.Folder:
                                    openUrl = "/files/" + objId;
                                    break;
                                case FolderObjectType.File:
                                    switch (dbSysFolderFile.File?.FileTypeId)
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
                return new FolderObjectReadResponseDTO()
                {
                    Id = parentFolderID,
                    Code = (int)Constants.ResponseReturnCode.Success,
                    Message = "success",
                    FolderObjects = objList
                };
            }
            catch (Exception ex)
            {
                // TODO: Log error
                string exMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }
                return new FolderObjectReadResponseDTO()
                {
                    Id = parentFolderID,
                    Code = (int)Constants.ResponseReturnCode.InternalError,
                    Message = "Failed to read folder:" + exMsg,
                    FolderObjects = new List<FolderObjectDTO>()
                };
            }
        }


        // TODO: Description update, return error DTO
        public async Task<FolderObjectUpdateResponseDTO> UpdateUsingID(
            int folderID, string newName, string newAttrs, string newDescription, int modifiedByUserName)
        {
            try
            {
                var vfolder = await dbContext.VFolders.Where(f => f.Id == folderID).FirstOrDefaultAsync();
                if (vfolder != null)
                {
                    var oldName = vfolder.Name;
                    var oldAttrs = vfolder.Attrs;
                    var oldDesc = vfolder.Description;
                    vfolder.Name = (newName.Trim() != "") ? newName: oldName;
                    vfolder.Attrs =( newAttrs.Trim() != "") ? newAttrs: oldAttrs;
                    vfolder.Description = (newDescription != "" ? newDescription: oldDesc);
                    vfolder.Path = Utils.ReplaceLastOccurrence(vfolder.Path, oldName, newName);
                    vfolder.LastUpdatedBy = modifiedByUserName;
                    vfolder.LastUpdatedDate = DateTime.Now;
                    // log                
                    dbContext.logMsg("FolderService", 0, $"Folder update", DBConstants.ADMIN_USER_ID,
                        $"Folder {oldName},{oldAttrs} updated to {vfolder.Name},{vfolder.Attrs}", vfolder.Id);
                    dbContext.SaveChanges();
                    return new FolderObjectUpdateResponseDTO
                    {
                        Id = folderID,
                        ObjectType = (int)DBConstants.FolderObjectType.Folder,
                        Code = (int)Constants.ResponseReturnCode.Success,
                        Message = "Folder updated"
                    };
                }
                else
                {
                    // Error
                    return new FolderObjectUpdateResponseDTO
                    {
                        Id = folderID,
                        ObjectType = (int)DBConstants.FolderObjectType.Folder,
                        Code = (int)Constants.ResponseReturnCode.Error,
                        Message = $"Folder does not exist"
                    };
                }
            }
            catch (Exception ex)
            {
                // TODO: Log error
                string exMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }
                return new FolderObjectUpdateResponseDTO
                {
                    Id = folderID,
                    ObjectType = (int)DBConstants.FolderObjectType.Folder,
                    Code = (int)Constants.ResponseReturnCode.InternalError,
                    Message = "Failed to update folder:" + exMsg
                };
            }
        }

        public async Task<FolderObjectUpdateResponseDTO> DeleteUsingID(int folderID, int deletedByUserID)
        {
            try
            {
                var dbObjList = await dbContext.SystemFolderFiles
                    .Include(sysff => sysff.Folder)                    
                    .Where(r => r.VFolderId == folderID && r.Rstatus == (byte)RStatus.Active)
                    .ToListAsync();
                if (dbObjList == null)
                {
                    // Error: should not happen
                    return new FolderObjectUpdateResponseDTO()
                    {
                        Id = folderID,
                        ObjectType = (int)DBConstants.FolderObjectType.Folder,
                        Code = (int)Constants.ResponseReturnCode.InternalError,
                        Message = $"Internal error",
                    };
                }
                else if (dbObjList.Count == 0)
                {
                    // Error: no folder with given ID
                    return new FolderObjectUpdateResponseDTO()
                    {
                        Id = folderID,
                        ObjectType = (int)DBConstants.FolderObjectType.Folder,
                        Code = (int)Constants.ResponseReturnCode.Error,
                        Message = $"Folder not found",
                    };
                }
                else if (dbObjList.Count > 1)
                {
                    // Error: impossible to have more than 1 mapping   
                    return new FolderObjectUpdateResponseDTO()
                    {
                        Id = folderID,
                        ObjectType = (int)DBConstants.FolderObjectType.Folder,
                        Code = (int)Constants.ResponseReturnCode.InternalError,
                        Message = $"Folder has multiple mappings",
                    };

                }                
                else
                {
                    // There is just 1 mapping as expected, try deleting                    
                    var sysff = dbObjList[0];
                    if (sysff.Folder != null)
                    {
                        sysff.Rstatus = (byte)RStatus.Inactive;   // soft delete the mapping
                        sysff.LastUpdatedBy = deletedByUserID;
                        sysff.LastUpdatedDate = DateTime.Now;
                        sysff.Folder.Rstatus = (byte)RStatus.Inactive; // soft delete the vfolder
                        sysff.Folder.LastUpdatedBy = deletedByUserID;
                        sysff.Folder.LastUpdatedDate = DateTime.Now;
                        dbContext.logMsg("FolderService", 0, $"Folder delete", DBConstants.ADMIN_USER_ID,
                             $"Folder id {sysff.VFolderId} deleted", sysff.VFolderId);
                        dbContext.SaveChanges();
                    }
                    else
                    {
                        return new FolderObjectUpdateResponseDTO()
                        {
                            Id = folderID,
                            ObjectType = (int)DBConstants.FolderObjectType.Folder,
                            Code = (int)Constants.ResponseReturnCode.InternalError,
                            Message = $"Folder is present in mapping but not in folders table",
                        };
                    }                    
                    
                }
                return new FolderObjectUpdateResponseDTO()
                {
                    Id = folderID,
                    ObjectType = (int)DBConstants.FolderObjectType.Folder,
                    Code = (int)Constants.ResponseReturnCode.Success,
                    Message = "Folder deleted",
                };
            }
            catch (Exception ex)
            {
                // TODO: Log error in file & do not return to user. The below code is only during dev.
                string exMsg = ex.Message;
                if (ex.InnerException != null) {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }
                return new FolderObjectUpdateResponseDTO
                {
                    Id = folderID,
                    ObjectType = (int)DBConstants.FolderObjectType.Folder,
                    Code = (int)Constants.ResponseReturnCode.InternalError,                    
                    Message = "Failed to delete folder:" + exMsg
                };
            }
        }
    }
}
