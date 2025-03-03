﻿using Aiursoft.Pylon.Models;
using Aiursoft.Pylon.Models.API;
using Aiursoft.Pylon.Models.Developer;
using Aiursoft.Pylon.Models.OSS;
using Aiursoft.Pylon.Models.Probe;
using Aiursoft.Pylon.Services;
using Aiursoft.Pylon.Services.ToAPIServer;
using Aiursoft.Pylon.Services.ToOSSServer;
using Aiursoft.Pylon.Services.ToProbeServer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Aiursoft.Developer.Models.AppsViewModels
{
    public class ViewAppViewModel : CreateAppViewModel
    {
        [Obsolete(message: "This method is only for framework", error: true)]
        public ViewAppViewModel() { }
        public static async Task<ViewAppViewModel> SelfCreateAsync(
            DeveloperUser user,
            App thisApp,
            CoreApiService coreApiService,
            OSSApiService ossApiService,
            AppsContainer appsContainer,
            SitesService sitesService)
        {
            var model = new ViewAppViewModel(user, thisApp);
            await model.Recover(user, thisApp, coreApiService, ossApiService, appsContainer, sitesService);
            return model;
        }

        public async Task Recover(
            DeveloperUser user,
            App thisApp,
            CoreApiService coreApiService,
            OSSApiService ossApiService,
            AppsContainer appsContainer,
            SitesService sitesService)
        {
            base.RootRecover(user, 1);
            var token = await appsContainer.AccessToken(thisApp.AppId, thisApp.AppSecret);

            var buckets = await ossApiService.ViewMyBucketsAsync(token);
            Buckets = buckets.Buckets;

            var grants = await coreApiService.AllUserGrantedAsync(token);
            Grants = grants.Grants;

            var sites = await sitesService.ViewMySitesAsync(token);
            Sites = sites.Sites;
        }

        private ViewAppViewModel(DeveloperUser user, App thisApp) : base(user)
        {
            if (thisApp.CreatorId != user.Id)
            {
                throw new InvalidOperationException("The app is not the user's app!");
            }
            AppName = thisApp.AppName;
            AppDescription = thisApp.AppDescription;
            AppCategory = thisApp.AppCategory;
            AppPlatform = thisApp.AppPlatform;
            AppId = thisApp.AppId;
            AppSecret = thisApp.AppSecret;
            EnableOAuth = thisApp.EnableOAuth;
            ForceInputPassword = thisApp.ForceInputPassword;
            ForceConfirmation = thisApp.ForceConfirmation;
            DebugMode = thisApp.DebugMode;
            PrivacyStatementUrl = thisApp.PrivacyStatementUrl;
            LicenseUrl = thisApp.LicenseUrl;
            AppIconAddress = thisApp.AppIconAddress;
            AppDomain = thisApp.AppDomain;
            ViewOpenId = thisApp.ViewOpenId;
            ViewPhoneNumber = thisApp.ViewPhoneNumber;
            ChangePhoneNumber = thisApp.ChangePhoneNumber;
            ConfirmEmail = thisApp.ConfirmEmail;
            ChangeBasicInfo = thisApp.ChangeBasicInfo;
            ChangePassword = thisApp.ChangePassword;
            ChangeGrantInfo = thisApp.ChangeGrantInfo;
        }

        public virtual bool JustHaveUpdated { get; set; } = false;
        public virtual string AppId { get; set; }
        public virtual string AppSecret { get; set; }
        public virtual string AppIconAddress { get; set; }
        [Url]
        [Display(Name = "Privacy Statement Url")]
        public virtual string PrivacyStatementUrl { get; set; }
        [Url]
        [Display(Name = "License Url")]
        public virtual string LicenseUrl { get; set; }

        [Display(Name = "Enable OAuth")]
        public virtual bool EnableOAuth { get; set; }
        [Display(Name = "Force Input Password")]
        public virtual bool ForceInputPassword { get; set; }
        [Display(Name = "Force Confirmation")]
        public virtual bool ForceConfirmation { get; set; }
        [Display(Name = "Debug Mode")]
        public virtual bool DebugMode { get; set; }
        [Display(Name = "App Domain")]
        public virtual string AppDomain { get; set; }
        // Permissions
        [Display(Name = "View user's basic identity info")]
        public bool ViewOpenId { get; set; } = true;
        [Display(Name = "View user's phone number")]
        public bool ViewPhoneNumber { get; set; }
        [Display(Name = "Change user's phone number")]
        public bool ChangePhoneNumber { get; set; }
        [Display(Name = "Change user's Email confirmation status")]
        public bool ConfirmEmail { get; set; }
        [Display(Name = "Change user's basic info like nickname and bio")]
        public bool ChangeBasicInfo { get; set; }
        [Display(Name = "Change user's password")]
        public bool ChangePassword { get; set; }
        [Display(Name = "Change user's other applications' grant status")]
        public bool ChangeGrantInfo { get; set; }

        public IEnumerable<Bucket> Buckets { get; set; } //= new List<Bucket>();
        public IEnumerable<Site> Sites { get; set; }
        public IEnumerable<Grant> Grants { get; set; }
        public IEnumerable<ViewAblePermission> ViewAblePermission { get; set; }
    }

    public class ViewAblePermission
    {
        public virtual int PermissionId { get; set; }
        public virtual string PermissionName { get; set; }
        public virtual bool Permitted { get; set; }
    }
}
