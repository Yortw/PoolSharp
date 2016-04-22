using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoolSharp
{
	internal static class ExceptionHelper
	{
		public static void ThrowYoureDoingItWrong()
		{
			throw new NotSupportedException("You're referencing the portal library. The portable library should only be referenced by other portable libraries. Please reference the specific assembly for your projects platform.");
		}
	}
}