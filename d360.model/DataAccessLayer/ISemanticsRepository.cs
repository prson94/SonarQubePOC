using d360.core.entities;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface ISemanticsRepository
    {
        Task<HttpStatusCode> DeleteSemanticAsync(string qualifier);
        Task<GetSemantics> GetSemanticsAsync(IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null);
        Task<List<GetSemantic>> GetSemanticVersionsByQualifierAsync(string qualifier, IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null);
        Task<List<GetSemantic>> PatchSemanticsAsync(List<PatchSemantic> semantics);
        Task<List<GetSemantic>> PostSemanticsAsync(List<PostSemantic> semantics);
        Task<List<GetSemantic>> PutSemanticsAsync(List<PutSemantic> semantics);
    }
}