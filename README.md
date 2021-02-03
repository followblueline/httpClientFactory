# httpClientFactory

HttpClient is intended to be instantiated once and re-used throughout the life of an application. 
Instantiating an HttpClient class for every request will exhaust the number of sockets and result in SocketException errors.

Multiple HttpClient instances in application will create a new connection pool.
Also the instance of HttpMessageHandler used by the HttpClient can't be changed.
Thus we are creating separate instance for different endpoints with separate handlers.

For .NET Core, use the HttpClientFactory with Microsoft's dependency injection. 
https://docs.microsoft.com/en-gb/dotnet/api/system.net.http.httpclient?view=netframework-4.7.1

https://stackoverflow.com/questions/48778580/singleton-httpclient-vs-creating-new-httpclient-request
