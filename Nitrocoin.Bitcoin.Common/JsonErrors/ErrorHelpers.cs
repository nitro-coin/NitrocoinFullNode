﻿using System.Collections.Generic;
using System.Net;

namespace Nitrocoin.Bitcoin.Common.JsonErrors
{
	public static class ErrorHelpers
    {
		public static ErrorResult BuildErrorResponse(HttpStatusCode statusCode, string message, string description)
		{
			ErrorResponse errorResponse = new ErrorResponse
			{
				Errors = new List<ErrorModel>
				{
					new ErrorModel
					{
						Status = (int) statusCode,
						Message = message,
						Description = description
					}
				}
			};

			return new ErrorResult((int)statusCode, errorResponse);
		}
	}
}
