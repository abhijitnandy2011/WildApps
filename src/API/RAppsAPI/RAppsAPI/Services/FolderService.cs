using Microsoft.EntityFrameworkCore;
using RAppsAPI.Data;
using RAppsAPI.Models;
using System.Collections.Generic;
using static RAppsAPI.Data.DBConstants;

namespace RAppsAPI.Services
{
    public class FolderService(RDBContext dbContext) : IFolderService
    {
        public Task<FolderObjectDTO?> Create(string folderName, string path)
        {
            throw new NotImplementedException();
        }

        public Task<FolderObjectDTO?> Create(string folderName, int parentFolderID)
        {
            throw new NotImplementedException();
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
                    .Include(sysff => sysff.ParentFolder)
                    .Where(r => r.VParentFolderId == parentFolderID)
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

                            objList.Add(new FolderObjectDTO
                            {
                                Id = (int)objId,
                                Name = (string)objName,
                                Description = "",   // Unused currently
                                Path = dbSysFolderFile.Folder?.Path ?? string.Empty,
                                ObjectType = (int)objType,
                                Attributes = objAttrs,
                                //IconURL = d,
                                CreatedBy       = objCreatedByUser,
                                CreatedDate     = objCreatedDate,
                                LastUpdatedBy   = objLastUpdatedByUser,
                                LastUpdatedDate = objLastUpdatedDate
                            });
                        }
                    }


                    

                }                
                return objList;
            }
            catch (Exception ex)
            {
                // Log error
                return new List<FolderObjectDTO>();
            }            
        }

       
    }
}
