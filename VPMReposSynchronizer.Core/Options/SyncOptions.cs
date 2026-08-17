namespace VPMReposSynchronizer.Core.Options;

public class SyncOptions
{
    public int MaxConcurrentTasks { get; set; } = 3;

    /// <summary>
    /// 每个包最多镜像的版本数（按版本号从新到旧取），0 表示全量镜像。
    /// </summary>
    public int MaxVersionsPerPackage { get; set; } = 0;

    /// <summary>
    /// 逗号分隔的仓库 ID 列表：列表内的仓库忽略 MaxVersionsPerPackage，始终全量镜像。
    /// 例：FullSyncRepoIds=liltoon,curated
    /// </summary>
    public string FullSyncRepoIds { get; set; } = string.Empty;
}
