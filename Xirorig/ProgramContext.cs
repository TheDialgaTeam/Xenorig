// Xirorig
// Copyright 2021 Yong Jian Ming
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using Xirorig.Options;

namespace Xirorig
{
    internal class ProgramContext
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IOptionsMonitor<XirorigOptions> _optionsMonitor;

        public CancellationToken ApplicationShutdownCancellationToken => _hostApplicationLifetime.ApplicationStopping;

        public XirorigOptions Options => _optionsMonitor.CurrentValue;

        public ILoggerTemplate<ProgramContext> Logger { get; }

        public ProgramContext(IHostApplicationLifetime hostApplicationLifetime, IOptionsMonitor<XirorigOptions> optionsMonitor, ILoggerTemplate<ProgramContext> logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _optionsMonitor = optionsMonitor;
            Logger = logger;
        }
    }
}