namespace Orchestrator.Models;

public class HttpPostRequest
{
    
    public string URL {get;set;}

    public Dictionary<string,string> Headers {get;set;}

    public string Body {get;set;}
}