namespace VPMReposSynchronizer.Core.Models.Types.RepoAdmin;

public class RepoAdminUpdateDto
{
    // 字段均为可空：POST 时由控制器手动校验必填项，PUT 支持部分字段更新
    public string? ApiId { get; set; }
    public string? UpstreamUrl { get; set; }
    public string? Description { get; set; }
    public string? SyncTaskCron { get; set; }

    /// <summary>
    /// 是否全量镜像所有版本（不受 Sync:MaxVersionsPerPackage 限制）。
    /// POST 时不传默认 false；PUT 时不传保持原值。
    /// </summary>
    public bool? FullSync { get; set; }
}
