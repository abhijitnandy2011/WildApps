﻿using Microsoft.AspNetCore.Identity;

namespace RAppsAPI.Entities
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;        
    }
}
