<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>Rock.Core.Newtonsoft</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

void Main()
{
	/*
	HttpClient is intended to be instantiated once and re-used throughout the life of an application. 
	Instantiating an HttpClient class for every request will exhaust the number of sockets and result in SocketException errors.

	Multiple HttpClient instances in application will create a new connection pool.
	Also the instance of HttpMessageHandler used by the HttpClient can't be changed.
	Thus we are creating separate instance for different endpoints with separate handlers.
	
	For .NET Core, use the HttpClientFactory with Microsoft's dependency injection. 
	https://docs.microsoft.com/en-gb/dotnet/api/system.net.http.httpclient?view=netframework-4.7.1

	https://stackoverflow.com/questions/48778580/singleton-httpclient-vs-creating-new-httpclient-request
	https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0
	https://stackoverflow.com/questions/10679214/how-do-you-set-the-content-type-header-for-an-httpclient-request
	http://httpbin.org/status/200
	http://httpbin.org/status/400
	http://httpbin.org/status/500
	http://httpbin.org/get?id=1&type=FOOD
	http://httpbin.org/json
	http://httpbin.org/stream/3?parent=foo&name=bar
	*/

	DoAsync();
}

private async void DoAsync()
{
	var res1 = await HttpBinHttpService.Get("get?id=1&type=FOOD");
	res1.Dump("Get");

	var data2 = JsonConvert.SerializeObject(new[]{
		new {
			path = "vehicles",
			method = "GET",
			reference_id = "vehicles",
			json = ""
		}
	});
	
	var res2 = await HttpBinHttpService.Post("post", data2);
	res2.Dump("Post");
	
	var res3 = await HttpBinHttpService.PostFormData("post", new Dictionary<string, string> { { "message", "Log entry" }});
	res3.Dump("PostFormData");
	
	var res4 = await ReqBinHttpService.Post("echo/post/json", data2);
	res4.Dump("ReqBinHttpService");
}

public class HttpBinHttpService : HttpClientServiceBase
{
	private static readonly string BASE_URL = "http://httpbin.org/";
	private static readonly string USER_AGENT = "HttpClientServiceBase";
	private static readonly int CLIENT_TIMEOUT_SECONDS = 120;
	private static HttpClient _client;

	static HttpBinHttpService()
	{
		_client = HttpClientServiceBase.CreateHttpClient();
		_client.BaseAddress = new Uri(BASE_URL);
		_client.DefaultRequestHeaders.Accept.Clear();
		_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		if (!string.IsNullOrWhiteSpace(USER_AGENT))
			_client.DefaultRequestHeaders.Add("User-Agent",	USER_AGENT);
		_client.Timeout = TimeSpan.FromSeconds(CLIENT_TIMEOUT_SECONDS);
		//$"HttpBinHttpService BASE_URL:{_client.BaseAddress} CREATED".Dump();
	}

	public static async Task<ResponseResult> Get(string uri)
	{
		return await HttpClientServiceBase.Get(_client, uri);
	}
	
	public static async Task<ResponseResult> Post(string uri, object json){
		return await HttpClientServiceBase.Post(_client, uri, json);
	}
	
	public static async Task<ResponseResult> PostFormData(string uri, Dictionary<string, string> formdata){
		return await HttpClientServiceBase.PostFormData(_client, uri, formdata);
	}
}

public class ReqBinHttpService : HttpClientServiceBase
{
	private static readonly string BASE_URL = "https://reqbin.com/";
	private static readonly string USER_AGENT = "HttpClientServiceBase";
	private static readonly int CLIENT_TIMEOUT_SECONDS = 120;
	private static HttpClient _client;

	static ReqBinHttpService()
	{
		_client = HttpClientServiceBase.CreateHttpClient();
		_client.BaseAddress = new Uri(BASE_URL);
		_client.DefaultRequestHeaders.Accept.Clear();
		_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		if (!string.IsNullOrWhiteSpace(USER_AGENT))
			_client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
		_client.Timeout = TimeSpan.FromSeconds(CLIENT_TIMEOUT_SECONDS);
		//$"HttpBinHttpService BASE_URL:{_client.BaseAddress} CREATED".Dump();
	}

	public static async Task<ResponseResult> Get(string uri)
	{
		return await HttpClientServiceBase.Get(_client, uri);
	}

	public static async Task<ResponseResult> Post(string uri, object json)
	{
		return await HttpClientServiceBase.Post(_client, uri, json);
	}

	public static async Task<ResponseResult> PostFormData(string uri, Dictionary<string, string> formdata)
	{
		return await HttpClientServiceBase.PostFormData(_client, uri, formdata);
	}
}

public class HttpClientServiceBase
{
	static HttpClientServiceBase()
	{
	}

	protected static HttpClient CreateHttpClient()
	{
		return new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
	}
	
	private static async Task<ResponseResult> ProcessResponse(HttpResponseMessage response)
	{
		var result = new ResponseResult();
		result.IsSuccessStatusCode = response.IsSuccessStatusCode;
		result.ReasonPhrase = response.ReasonPhrase;
		result.StatusCode = (int)response.StatusCode;
		result.Content = await response.Content.ReadAsStringAsync();
		return result;
	}

	public static async Task<ResponseResult> Get(HttpClient client, string uri)
	{
		var uriIncoming = new Uri(uri, UriKind.Relative); // Don't let the user specify absolute urls.
		var uriOutgoing = new Uri(client.BaseAddress, uriIncoming);
		var request = new HttpRequestMessage(HttpMethod.Get, uriOutgoing);
		//request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); // already set in constructor
		
		var response = await client.SendAsync(request);
		var result = await ProcessResponse(response);
		return result;
	}

	public static async Task<ResponseResult> Post(HttpClient client, string uri, object json)
	{
		var uriIncoming = new Uri(uri, UriKind.Relative); // Don't let the user specify absolute urls.
		var uriOutgoing = new Uri(client.BaseAddress, uriIncoming);

		var jcontent = JsonConvert.SerializeObject(json);
		//var buffer = System.Text.Encoding.UTF8.GetBytes(jcontent);
		//var byteContent = new ByteArrayContent(buffer);

		HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uriOutgoing);
		request.Content = new StringContent(jcontent, Encoding.UTF8, "application/json");//CONTENT-TYPE header

		HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
		var result = await ProcessResponse(response);
		return result;
	}

	public static async Task<ResponseResult> PostFormData(HttpClient client, string uri, Dictionary<string, string> formdata)
	{
		var uriIncoming = new Uri(uri, UriKind.Relative); // Don't let the user specify absolute urls.
		var uriOutgoing = new Uri(client.BaseAddress, uriIncoming);

		HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uriOutgoing);
		request.Content = new FormUrlEncodedContent(formdata);
		
		HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
		var result = await ProcessResponse(response);
		return result;
	}
	
	// https://stackoverflow.com/questions/372865/path-combine-for-urls
	public static string UrlCombine(string url1, string url2)
	{
		if (url1.Length == 0)
		{
			return url2;
		}

		if (url2.Length == 0)
		{
			return url1;
		}

		url1 = url1.TrimEnd('/', '\\');
		url2 = url2.TrimStart('/', '\\');

		return string.Format("{0}/{1}", url1, url2);
	}
	
	public class ResponseResult
	{
		public int StatusCode { get; set; } = 0;
		public string ReasonPhrase { get; set; }
		public string Content { get; set; }
		public bool IsSuccessStatusCode { get; set; }
	}

}
