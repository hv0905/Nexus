﻿using Aiursoft.Developer.Models.AppsViewModels;
using Aiursoft.Pylon.Models.Developer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aiursoft.Developer.Models.SitesViewModels
{
    public class NewFileViewModel : AppLayoutModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public NewFileViewModel() { }
        public NewFileViewModel(DeveloperUser user) : base(user, 5) { }
        public void Recover(DeveloperUser user)
        {
            RootRecover(user, 5);
        }

        public bool ModelStateValid { get; set; } = true;
        public string AppId { get; set; }
        public string SiteName { get; set; }
        public string Path { get; set; }
    }
}
