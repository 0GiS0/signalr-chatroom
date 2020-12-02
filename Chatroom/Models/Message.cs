using System;
using System.Collections.Generic;
using Newtonsoft.Json;
public class Message
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "chatroomid")]
    public string ChatRoomId { get; set; }

    [JsonProperty(PropertyName = "username")]
    public string UserName { get; set; }

    [JsonProperty(PropertyName = "videotime")]
    public decimal VideoTime { get; set; }

    [JsonProperty(PropertyName = "date")]
    public DateTime Date { get; set; }

    [JsonProperty(PropertyName = "text")]
    public string Text { get; set; }

    [JsonProperty(PropertyName = "ugly")]
    public bool Ugly { get; set; }

    [JsonProperty(PropertyName = "terms")]
    public List<string> Terms { get; set; }
}