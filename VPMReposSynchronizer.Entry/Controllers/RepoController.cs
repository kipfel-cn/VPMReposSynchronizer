using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Cronos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using VPMReposSynchronizer.Core.Models.Entity;
using VPMReposSynchronizer.Core.Models.Types;
using VPMReposSynchronizer.Core.Models.Types.RepoAdmin;
using VPMReposSynchronizer.Core.Models.Types.RepoBrowser;
using VPMReposSynchronizer.Core.Services;

namespace VPMReposSynchronizer.Entry.Controllers;

[ApiController]
[Route("repos")]
[Produces("application/json")]
[OutputCache(PolicyName = "vpm")]
public class RepoController(
    RepoBrowserService repoBrowserService,
    RepoMetaDataService repoMetaDataService,
    IMapper mapper) : ControllerBase
{
    [Route("")]
    [HttpGet]
    [ProducesResponseType<PageResult<BrowserRepo>>(StatusCodes.Status200OK)]
    public async Task<PageResult<BrowserRepo>> GetRepos([Range(0, int.MaxValue)] int page = 0,[Range(0, 50)] int count = 10)
    {
        return await repoBrowserService.GetReposAsync(page, count);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(AuthenticationSchemes = "ApiKey", Policy = "ApiKey")]
    public async Task<IActionResult> Create(RepoAdminUpdateDto repo)
    {
        // DTO 字段可空（为支持 PUT 部分更新），POST 时手动校验必填项
        if (string.IsNullOrEmpty(repo.ApiId) || string.IsNullOrEmpty(repo.UpstreamUrl) ||
            string.IsNullOrEmpty(repo.SyncTaskCron))
            return BadRequest("apiId, upstreamUrl and syncTaskCron are required.");

        var repoEntity = mapper.Map<VpmRepoEntity>(repo);

        if (!Uri.TryCreate(repoEntity.UpStreamUrl, UriKind.Absolute, out _)) return BadRequest("Invalid url.");

        if (!CronExpression.TryParse(repoEntity.SyncTaskCron, out _)) return BadRequest("Invalid cron expression.");

        await repoMetaDataService.AddRepoAsync(repoEntity);
        return NoContent();
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(AuthenticationSchemes = "ApiKey", Policy = "ApiKey")]
    public async Task<IActionResult> Update(string id, RepoAdminUpdateDto repo)
    {
        if (await repoMetaDataService.GetRepoById(id) is not { } repoEntity) return NotFound();

        // 支持部分字段更新：不传的字段保持原值
        if (repo.Description is not null) repoEntity.Description = repo.Description;

        if (repo.UpstreamUrl is not null)
        {
            if (!Uri.TryCreate(repo.UpstreamUrl, UriKind.Absolute, out _)) return BadRequest("Invalid url.");
            repoEntity.UpStreamUrl = repo.UpstreamUrl;
        }

        if (repo.SyncTaskCron is not null)
        {
            if (!CronExpression.TryParse(repo.SyncTaskCron, out _)) return BadRequest("Invalid cron expression.");
            repoEntity.SyncTaskCron = repo.SyncTaskCron;
        }

        if (repo.FullSync.HasValue) repoEntity.FullSync = repo.FullSync.Value;

        await repoMetaDataService.UpdateRepoAsync(repoEntity);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(AuthenticationSchemes = "ApiKey", Policy = "ApiKey")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!await repoMetaDataService.IsRepoExist(id)) return NotFound();

        await repoMetaDataService.DeleteRepoAsync(id);

        return NoContent();
    }

    [Route("{id}")]
    [HttpGet]
    [ProducesResponseType<BrowserRepo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRepo(string id)
    {
        var repo = await repoBrowserService.GetRepoAsync(id);

        if (repo is null) return NotFound();

        return Ok(repo);
    }

    [Route("{repoId}/packages")]
    [HttpGet]
    [ProducesResponseType<PageResult<BrowserPackage>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResult<BrowserPackage>>> GetAllPackage(string repoId, [Range(0, int.MaxValue)] int page = 0, [Range(0, 50)] int count = 10)
    {
        if (await repoBrowserService.GetRepoAsync(repoId) is null) return NotFound();

        var packages = await repoBrowserService.GetPackagesAsync(repoId, count, page);

        return Ok(packages);
    }

    [Route("{repoId}/packages/{packageId}")]
    [HttpGet]
    [ProducesResponseType<BrowserPackage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackage(string repoId, string packageId)
    {
        var package = await repoBrowserService.GetPackageAsync(repoId, packageId);
        if (package == null) return NotFound();

        return Ok(package);
    }
}
