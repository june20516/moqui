namespace Moqui.Core.Data
{
    /// <summary>
    /// data/ 폴더의 텍스트 파일을 읽는 포트. 경로는 data/ 기준 상대 경로이며 구분자는 '/'를 쓴다 (예: "levels/stage01.json").
    /// </summary>
    public interface IDataSource
    {
        string ReadText(string relativePath);
    }
}
