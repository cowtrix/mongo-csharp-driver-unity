/* Copyright 2019-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    internal class DnsClientWrapper : IDnsResolver
    {
        public async UniTask<List<SrvRecord>> ResolveSrvRecordsAsync(string service, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(service, nameof(service));
            var response = await NetworkManager.Instance.ResolveDNS(service, "SRV");
            return GetSrvRecords(response);
        }

        public async UniTask<List<TxtRecord>> ResolveTxtRecordsAsync(string domainName, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(domainName, nameof(domainName));
            var response = await NetworkManager.Instance.ResolveDNS(domainName, "TXT");
            return GetTxtRecords(response);
        }

        private List<SrvRecord> GetSrvRecords(DnsResponse response)
        {
            var srvRecords = new List<SrvRecord>();

            foreach (var record in response.Answer)
            {
                if (record.type == 33 && record.data.Split(' ').Length == 4)
                {
                    var parts = record.data.Split(' ');
                    var host = parts[3].TrimEnd('.');
                    var port = int.Parse(parts[2]);
                    var ttl = TimeSpan.FromSeconds(record.ttl);

                    srvRecords.Add(new SrvRecord(new DnsEndPoint(host, port), ttl));
                }
            }

            return srvRecords;
        }

        private List<TxtRecord> GetTxtRecords(DnsResponse response)
        {
            var txtRecords = new List<TxtRecord>();

            foreach (var record in response.Answer)
            {
                if (record.type == 16)
                {
                    txtRecords.Add(new TxtRecord(new List<string> { record.data }));
                }
            }

            return txtRecords;
        }

        // Sync methods not supported in WebGL - throw explicit error
        public List<SrvRecord> ResolveSrvRecords(string service, CancellationToken cancellationToken)
        {
            throw new System.NotSupportedException("Synchronous DNS resolution not supported in WebGL");
        }

        public List<TxtRecord> ResolveTxtRecords(string domainName, CancellationToken cancellationToken)
        {
            throw new System.NotSupportedException("Synchronous DNS resolution not supported in WebGL");
        }
    }

}
