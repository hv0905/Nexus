using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Attributes;
using Aiursoft.Pylon.Models.Stargate.ChannelAddressModels;
using Aiursoft.Stargate.Data;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Pylon.Models.Stargate;
using Aiursoft.Pylon.Models.Stargate.ChannelViewModels;
using Aiursoft.Pylon.Models;
using Aiursoft.Pylon.Services;
using Aiursoft.Pylon.Models.Stargate.ListenAddressModels;

namespace Aiursoft.Stargate.Controllers
{
    [APIExpHandler]
    [APIModelStateChecker]
    public class ChannelController : Controller
    {
        private readonly StargateDbContext _dbContext;
        private readonly ACTokenManager _tokenManager;

        public ChannelController(
            StargateDbContext dbContext,
            ACTokenManager tokenManager)
        {
            _dbContext = dbContext;
            _tokenManager = tokenManager;
        }

        [APIProduces(typeof(ViewMyChannelsViewModel))]
        public async Task<IActionResult> ViewMyChannels(ViewMyChannelsAddressModel model)
        {
            var appid = _tokenManager.ValidateAccessToken(model.AccessToken);
            var appLocal = await _dbContext.Apps.SingleOrDefaultAsync(t => t.Id == appid);
            if (appLocal == null)
            {
                appLocal = new StargateApp
                {
                    Id = appid,
                    Channels = new List<Channel>()
                };
                _dbContext.Apps.Add(appLocal);
                await _dbContext.SaveChangesAsync();
            }
            var channels = await _dbContext
                .Channels
                .Where(t => t.AppId == appid)
                .ToListAsync();
            var viewModel = new ViewMyChannelsViewModel
            {
                AppId = appLocal.Id,
                Channel = channels,
                Code = ErrorType.Success,
                Message = "Successfully get your channels!"
            };
            return Json(viewModel);
        }

        public async Task<IActionResult> ValidateChannel(ChannelAddressModel model)
        {
            var channel = await _dbContext.Channels.FindAsync(model.Id);
            if (channel == null)
            {
                return Json(new AiurProtocol
                {
                    Code = ErrorType.NotFound,
                    Message = "Can not find your channel!"
                });
            }
            if (channel.ConnectKey != model.Key)
            {
                return Json(new AiurProtocol
                {
                    Code = ErrorType.Unauthorized,
                    Message = "Wrong connection key!"
                });
            }
            else
            {
                return this.Protocol(ErrorType.Success, "Current Info.");
            }
        }

        [HttpPost]
        [APIProduces(typeof(CreateChannelViewModel))]
        public async Task<IActionResult> CreateChannel([FromForm]CreateChannelAddressModel model)
        {
            //Update app info
            var appid = _tokenManager.ValidateAccessToken(model.AccessToken);
            var appLocal = await _dbContext.Apps.Include(t => t.Channels).SingleOrDefaultAsync(t => t.Id == appid);
            if (appLocal == null)
            {
                appLocal = new StargateApp
                {
                    Id = appid,
                    Channels = new List<Channel>()
                };
                _dbContext.Apps.Add(appLocal);
            }
            //Create and save to database
            var newChannel = new Channel
            {
                Description = model.Description,
                ConnectKey = Guid.NewGuid().ToString("N")
            };
            appLocal.Channels.Add(newChannel);
            await _dbContext.SaveChangesAsync();
            //return model
            var viewModel = new CreateChannelViewModel
            {
                ChannelId = newChannel.Id,
                ConnectKey = newChannel.ConnectKey,
                Code = ErrorType.Success,
                Message = "Successfully created your channel!"
            };
            return Json(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteChannel([FromForm]DeleteChannelAddressModel model)
        {
            var appid = _tokenManager.ValidateAccessToken(model.AccessToken);
            var channel = await _dbContext.Channels.FindAsync(model);
            if (channel.AppId != appid)
            {
                return Json(new AiurProtocol { Code = ErrorType.Unauthorized, Message = "The channel you try to delete is not your app's channel!" });
            }
            _dbContext.Channels.Remove(channel);
            await _dbContext.SaveChangesAsync();
            return Json(new AiurProtocol { Code = ErrorType.Success, Message = "Successfully deleted your channel!" });
        }

        /// <summary>
        /// This action will delete all channels he created!
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeleteApp([FromForm]DeleteAppAddressModel model)
        {
            var appid = _tokenManager.ValidateAccessToken(model.AccessToken);
            if (appid != model.AppId)
            {
                return Json(new AiurProtocol { Code = ErrorType.Unauthorized, Message = "The app you try to delete is not the accesstoken you granted!" });
            }
            var target = await _dbContext.Apps.FindAsync(appid);
            if (target != null)
            {
                _dbContext.Channels.Delete(t => t.AppId == target.Id);
                _dbContext.Apps.Remove(target);
                await _dbContext.SaveChangesAsync();
                return Json(new AiurProtocol { Code = ErrorType.Success, Message = "Successfully deleted that app and all channels." });
            }
            return Json(new AiurProtocol { Code = ErrorType.HasDoneAlready, Message = "That app do not exists in our database." });
        }
    }
}