package org.nhindirect.config.store;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.FetchType;
import javax.persistence.GeneratedValue;
import javax.persistence.GenerationType;
import javax.persistence.Id;
import javax.persistence.JoinColumn;
import javax.persistence.ManyToOne;
import javax.persistence.Table;

@Entity
@Table(name = "trustbundledomainreltn")
public class TrustBundleDomainReltn 
{
	private long id;
	
	private Domain domain;
	
	private TrustBundle trustBundle;
	
	public TrustBundleDomainReltn()
	{
		
	}
	
    /**
     * Get the value of id.
     * 
     * @return the value of id.
     */
    @Column(name = "id", nullable = false)
    @Id
    @GeneratedValue(strategy = GenerationType.AUTO)
    public long getId() 
    {
        return id;
    }
    
    /**
    * Set the value of id.
    * 
    * @param id
    *            The value of id.
    */
   public void setId(long id) 
   {
       this.id = id;
   }   
   
   /**
    * Gets the value of the trust bundle.
    * 
    * @return The value of the trust bundle.
    */
   @ManyToOne(optional = false, fetch = FetchType.EAGER)
   @JoinColumn(name = "trust_bundle_id")
   public TrustBundle getTrustBundle() 
   {
       return trustBundle;
   }
   
   /**
    * Sets the value of the trust bundle.
    * 
    * @param bundle The value of the trust bundle.
    */
   public void setTrustBundle(TrustBundle bundle)
   {
	   this.trustBundle = bundle;
   }
   
   
   /**
    * Gets the value of the domain.
    * 
    * @return The value of the domain.
    */
   @ManyToOne(optional = false, fetch = FetchType.EAGER)
   @JoinColumn(name = "domain_id")
   public Domain getDomain() 
   {
       return domain;
   }
   
   /**
    * Sets the value of the domain.
    * 
    * @param bundle The value of the domain.
    */
   public void setDomain(Domain domain)
   {
	   this.domain = domain;
   }   
}