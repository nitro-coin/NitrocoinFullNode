using Microsoft.AspNetCore.Mvc;

namespace Nitrocoin.Bitcoin.Common.JsonErrors
{
    public class ErrorResult : ObjectResult
    {
	    public ErrorResult(int statusCode, ErrorResponse value) : base(value)
	    {
			StatusCode = statusCode;
		}
	}
}
