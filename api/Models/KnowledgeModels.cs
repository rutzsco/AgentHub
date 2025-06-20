using System.ComponentModel.DataAnnotations;

namespace AgentHub.Api.Models;

public class KnowledgeRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    public string IndexName { get; set; } = string.Empty;
    
    public Dictionary<string, object>? SecurityFilters { get; set; }
    
    public string? Title { get; set; }
    
    public string? Category { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

public class KnowledgeDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Content { get; set; } = string.Empty;
    
    public string? Title { get; set; }
    
    public string? Category { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Dictionary<string, object>? SecurityFilters { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

public class KnowledgeResponse
{
    public string Id { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    public string? Message { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class KnowledgeSearchRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;
    
    [Required]
    public string IndexName { get; set; } = string.Empty;
    
    public int Top { get; set; } = 5;
    
    public Dictionary<string, object>? SecurityFilters { get; set; }
    
    public string[]? Categories { get; set; }
    
    public bool IncludeContent { get; set; } = true;
}

public class KnowledgeSearchResult
{
    public string Id { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public string? Title { get; set; }
    
    public string? Category { get; set; }
    
    public double Score { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

public class KnowledgeSearchResponse
{
    public List<KnowledgeSearchResult> Results { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public string? Message { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string Query { get; set; } = string.Empty;
}
