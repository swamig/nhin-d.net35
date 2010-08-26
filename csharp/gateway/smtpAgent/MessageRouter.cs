﻿/* 
 Copyright (c) 2010, NHIN Direct Project
 All rights reserved.

 Authors:
    Umesh Madan     umeshma@microsoft.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of the The NHIN Direct Project (nhindirect.org). nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using NHINDirect.Agent;
using NHINDirect.Config.Store;

namespace NHINDirect.SmtpAgent
{    
    public class MessageRoute : MessageProcessingSettings
    {
        public MessageRoute()
        {
        }
        
        [XmlElement]
        public string AddressType
        {
            get;          
            set;
        }

        public override void Validate()
        {
            base.Validate();
            if (string.IsNullOrEmpty(this.AddressType))
            {
                throw new ArgumentException("Missing address type");
            }
        }
    }

    public class MessageRouter : IEnumerable<MessageRoute>
    {
        AgentDiagnostics m_diagnostics;
        Dictionary<string, MessageRoute> m_routes;   // addressType, messageRouteSettings

        internal MessageRouter(AgentDiagnostics diagnostics)
        {
            m_diagnostics = diagnostics;
            m_routes = new Dictionary<string, MessageRoute>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Return the route for an address type
        /// </summary>
        public MessageRoute this[string addressType]
        {
            get
            {
                MessageRoute settings = null;
                if (!m_routes.TryGetValue(addressType ?? string.Empty, out settings))
                {
                    settings = null;
                }
                return settings;
            }
        }

        public void SetRoutes(IEnumerable<MessageRoute> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }

            foreach (MessageRoute setting in settings)
            {
                setting.EnsureFolders();
                m_routes[setting.AddressType] = setting;
            }
        }
        
        public bool Route(ISmtpMessage message, MessageEnvelope envelope, Action<ISmtpMessage, MessageRoute> action)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException();
            }
            
            return this.Route(message, envelope.DomainRecipients, action);
        }
        
        // Returns true if all recipients had routes assigned
        public bool Route(ISmtpMessage message, IEnumerable<NHINDAddress> recipients, Action<ISmtpMessage, MessageRoute> action)
        {
            if (recipients == null || action == null)
            {
                throw new ArgumentException();
            }

            int countRouted = 0;
            int recipientCount = 0;
            foreach (NHINDAddress recipient in recipients)
            {
                ++recipientCount;
                Address address = recipient.Tag as Address;
                if (address != null && !string.IsNullOrEmpty(address.Type))
                {
                    MessageRoute route = m_routes[address.Type];
                    if (route != null)
                    {
                        action(message, route);
                        ++countRouted;
                    }
                }
            }

            return (countRouted == recipientCount);
        }

        public IEnumerator<MessageRoute> GetEnumerator()
        {
            return m_routes.Values.GetEnumerator();
        }

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }

}