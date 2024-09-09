public class FileUploadJob
{
    private readonly IRepository _repo;
    private readonly IFileGenerateService _fileGenerator;
    private readonly ISftpService _sftpService;
    private readonly ISmtpService _smtpService;

    public FileUploadJob(
        IRepository repo,
        IFileGenerateService fileGenerator,
        ISftpService sftpService,
        ISmtpService smtpService
    )
    {
        _repo = repo;
        _fileGenerator = fileGenerator;
        _sftpService = sftpService;
        _smtpService = smtpService;
    }

    public async Task UploadAsync()
    {
        // create Task for repository
        var repoTask = new List<Task<string>>();

        // get each data from repository then add to task
        foreach (var file in GetFilesName())
        {
            repoTask.Add(_repo.GetData(file));
        }

        // make sure to all tasks are finished
        var result = Task.WhenAll(repoTask);

        // create Task for file generate
        var fileTask = new List<Task<string>>();

        // create each file base on get data from repository
        foreach (var str in result)
        {
            fileTask.Add(_fileGenerator.Generate(str));
        }

        // make sure ever file was generated
        var filesName = Task.WhenAll(fileTask);

        // create Task for SFTP service
        var sftpTask = new List<Task>();

        // upload each created file to SFTP server
        foreach (var file in filesName)
        {
            sftpTask.Add(_sftpService.UploadToFileServer(file));
        }

        // make sure every file was uploaded to server
        var sendFile = Task.WhenAll(sftpTask);

        // create Task for email service
        var smtpTask = new List<Task>();

        // send email about created / uploaded files
        foreach (var item in sendFile)
        {
            smtpTask.Add(_smtpService.SendEmail(item));
        }
      
        // make sure all email was send
        await Task.WhenAll(smtpTask);
    }
}
