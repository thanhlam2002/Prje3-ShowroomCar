using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireEmployee")]
    public class DocumentsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public DocumentsController(ShowroomDbContext db) => _db = db;

        private static string NewNo(string pfx) => $"{pfx}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        public class DocumentCreateRequest
        {
            public string DocType { get; set; } = null!;
            public DateOnly DocDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
            public long? CustomerId { get; set; }
            public long? RelatedId { get; set; }
            public string? RelatedTable { get; set; }
            public string StorageUrl { get; set; } = null!;
        }

        public class LinkRequest
        {
            public long DocId { get; set; }
            public string EntityTable { get; set; } = null!;
            public long EntityId { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create(DocumentCreateRequest req)
        {
            var doc = new Document
            {
                DocNo = NewNo("DOC"),
                DocType = req.DocType,
                DocDate = req.DocDate,
                CustomerId = req.CustomerId,
                RelatedId = req.RelatedId,
                RelatedTable = req.RelatedTable,
                StorageUrl = req.StorageUrl,
                CreatedBy = null,
                CreatedAt = DateTime.UtcNow
            };
            _db.Documents.Add(doc);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = doc.DocId }, new { docId = doc.DocId, doc.DocNo });
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<Document>> Get(long id)
        {
            var d = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.DocId == id);
            return d == null ? NotFound() : Ok(d);
        }

        [HttpPost("link")]
        public async Task<ActionResult> Link(LinkRequest req)
        {
            var d = await _db.Documents.FindAsync(req.DocId);
            if (d == null) return NotFound("Document not found.");

            var link = new DocumentLink
            {
                DocId = req.DocId,
                EntityTable = req.EntityTable,
                EntityId = req.EntityId
            };
            _db.DocumentLinks.Add(link);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
