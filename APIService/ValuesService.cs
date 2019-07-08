using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace APIService
{
    /*
     * TEST
        HTTP MEATHODS:
            GET
            POST
            PUT
            DELETE
        MIDDLEWARE//AUTHENTICATION
        
         */
    [RegisterServiceAttribute("Values")]
    public class ValuesService
    {
        static Dictionary<string, Person> store = new Dictionary<string, Person>();
       
        //GET and POST
        public Person GetPerson(string id)
        {
            if (store.ContainsKey(id))
            {
                return store[id];
            }
            return null;
        }

        //POST
        public Person AddPerson(Person person)//single param
        {
            store.Add(person.Id, person);
            return person;
        }
        public Person AddPersonWithAddress(Person person, Address address)//multiple param
        {
            return AddPerson(person);
        }
        public Person AddAddess(PersonAddress personAddress)
        {
            return AddPerson(personAddress.Person);

        }

    }

    public class PersonAddress
    {
        public Person Person { get; set; }
        public Address Address { get; set; }
    }
    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class Address
    {
        public string Id { get; set; }
        public string Street { get; set; }
    }
}
