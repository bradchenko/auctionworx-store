using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using RainWorx.FrameWorx.Clients;
using RainWorx.FrameWorx.DTO;
using RainWorx.FrameWorx.DTO.FaultContracts;
using RainWorx.FrameWorx.MVC.Areas.API.Controllers.Base;
using RainWorx.FrameWorx.MVC.Areas.API.Controllers.Helpers;
using RainWorx.FrameWorx.MVC.Areas.API.Models;
using RainWorx.FrameWorx.Utility;

//using RainWorx.FrameWorx.WebAPI.Models;

namespace RainWorx.FrameWorx.MVC.Areas.API.Controllers
{
    /// <summary>
    /// Provides services to Create/Update/Get/Delete Listings
    /// </summary>
    [RoutePrefix("api/event")]
    public class EventController : AuctionWorxAPIController
    {
        /// <summary>
        /// Gets an Event by ID
        /// </summary>
        /// <param name="id">The ID of the event to get</param>
        /// <returns>An HTTP Status code of 200 (OK) and the Event on success.  Otherwise, HTTP Status code 404 (Not Found) if the event is not found.</returns>
        [Route("{id}")]
        [ResponseType(typeof(APIEvent))]
        public HttpResponseMessage Get(int id)
        {
            try
            {
                string userName = Request.GetUserName();
                DTO.Event auctionEvent = EventClient.GetEventByIDWithFillLevel(userName, id, Strings.EventFillLevels.All);
                APIEvent smallEvent = APIEvent.FromDTOEvent(auctionEvent);
                Utilities.PruneEventCustomFieldsVisbility(ref smallEvent, userName);
                return Request.CreateResponse<APIEvent>(HttpStatusCode.OK, smallEvent);
            }
            catch (System.ServiceModel.FaultException<InvalidArgumentFaultContract> iafc)
            {
                //let the redirect below handle the "Event doesn't exist" error, otherwise re-throw the exception
                if (iafc.Detail.Reason != ReasonCode.EventNotExist) throw iafc;
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Event not found");
            }
        }

        ///// <summary>
        ///// Searches for listings
        ///// </summary>        
        ///// <param name="page">The zero-based page number to retrieve</param>
        ///// <param name="size">The size of a single page</param>
        ///// <returns>An HTTP Status code of 200 (OK) and the Listing on success.  Otherwise, HTTP Status code 404 (Not Found) if the Listing is not found.</returns>
        //[Route("search/{page}/{size}")]
        //[ResponseType(typeof(Page<APIListing>))]
        //public HttpResponseMessage GetSearchListings(int page, int size)
        //{
        //    string userName = Request.GetUserName();

        //    ListingPageQuery query = new ListingPageQuery();
        //    query.Descending = true;
        //    query.Index = 0;
        //    query.Name = "Search";
        //    query.Input = new UserInput(userName, userName);
        //    query.Input.Items = Request.GetQueryNameValuePairs()
        //        .ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

        //    Page<Listing> results = ListingClient.SearchListings(userName, query, page, size);

        //    User user = UserClient.GetUserByUserName(userName, userName);

        //    Page<APIListing> retVal = new Page<APIListing>(new List<APIListing>(results.List.Count), results.PageIndex,
        //        results.PageSize, results.TotalItemCount, results.SortExpression);
        //    foreach (Listing listing in results.List)
        //    {
        //        APIListing smallListing = APIListing.FromDTOListing(listing);
        //        Utilities.PruneListingCustomFieldsVisbility(ref smallListing, user);
        //        retVal.List.Add(smallListing);
        //    }

        //    return Request.CreateResponse<Page<APIListing>>(HttpStatusCode.OK, retVal);
        //}

        /// <summary>
        /// Creates an Event
        /// </summary>
        /// <param name="input">A UserInput object containing the data with which to create the Event</param>
        /// <returns>An HTTP Status code of 201 (Created) upon success, with the Location response header set to the location of the newly created Event (the GET resource).</returns>
        [Route("")]
        [ResponseType(typeof(string))]
        public HttpResponseMessage Post([FromBody] UserInput input)
        {
            if (input == null) return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "UserInput is null");
            int newEventID = 0;

            //if (!input.Items.ContainsKey("AllCategories") || string.IsNullOrEmpty(input.Items["AllCategories"]))
            //{
            //    var lineages = new List<string>();
            //    int categoryID = 0;
            //    if (input.Items.ContainsKey("CategoryID") && int.TryParse(input.Items["CategoryID"], out categoryID) && categoryID > 0)
            //    {
            //        lineages.Add(CommonClient.GetCategoryPath(categoryID).Trees[categoryID].LineageString);
            //    }
            //    int regionID = 0;
            //    if (input.Items.ContainsKey("RegionID") && int.TryParse(input.Items["RegionID"], out regionID) && regionID > 0)
            //    {
            //        lineages.Add(CommonClient.GetCategoryPath(regionID).Trees[regionID].LineageString);
            //    }
            //    if (lineages.Count > 0)
            //    {
            //        string categories = Hierarchy<int, Category>.MergeLineageStrings(lineages);
            //        input.Items.Add(Strings.Fields.AllCategories, categories);
            //    }
            //}

            if (SiteClient.EnableEvents)
            {
                newEventID = EventClient.CreateEvent(Request.GetUserName(), input);
            }
            return Request.Created(newEventID);
        }

        /// <summary>
        /// Deletes an Event.
        /// </summary>
        /// <param name="id">The ID of the Event to delete</param>
        /// <returns>An HTTP Status code of 204 (No Content) upon success.</returns>
        [Route("{id}")]
        public HttpResponseMessage Delete(int id)
        {
            try
            {
                //ListingClient.DeleteListing(Request.GetUserName(), id);
                EventClient.DeleteEvent(Request.GetUserName(), id);
            }
            catch (System.ServiceModel.FaultException<InvalidArgumentFaultContract> iafc)
            {
                if (iafc.Detail.Reason != ReasonCode.EventNotExist) throw iafc;
            }
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Updates an Event
        /// </summary>
        /// <param name="input">A UserInput object containing the data with which to update the Event</param>
        /// <returns>An HTTP Status code of 204 (No Content) upon success.</returns>
        [Route("")]
        public HttpResponseMessage Put([FromBody] UserInput input)
        {
            string username = Request.GetUserName();

            int eventId = 0;
            if (input.Items.ContainsKey(Strings.Fields.Id) && int.TryParse(input.Items[Strings.Fields.Id], out eventId))
            {
                //event ID specified properly
                //DTO.Event auctionEvent;
                //try
                //{
                //    auctionEvent = EventClient.GetEventByIDWithFillLevel(username, eventId, Strings.EventFillLevels.All);
                //}
                //catch (System.ServiceModel.FaultException<InvalidArgumentFaultContract> iafc)
                //{
                //    //let the redirect below handle the "Event doesn't exist" error, otherwise re-throw the exception
                //    if (iafc.Detail.Reason != ReasonCode.EventNotExist) throw iafc;
                //    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Event not found");
                //}

                EventClient.UpdateEvent(username, input, false);
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            else
            {
                //Event ID missing or not specified properly
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                                              new ErrorResponse()
                                                  {
                                                      Message =
                                                          "Id missing from UserInput or not convertable to an int."
                                                  });
            }
        }

        ///// <summary>
        ///// Retrieves all registered listing types
        ///// </summary>
        ///// <returns>a list of listing types</returns>
        //[Route("types")]
        //[ResponseType(typeof(List<ListingType>))]
        //public HttpResponseMessage GetListingTypes()
        //{
        //    return Request.CreateResponse(HttpStatusCode.OK, ListingClient.ListingTypes);
        //}

        ///// <summary>
        ///// Retrieves all properties for a provided listing type name
        ///// </summary>        
        ///// <param name="name">The name of the listing type to get properties for</param>
        ///// <returns>A list of CustomProperties for the specified listing type</returns>
        //[Route("type/{name}")]
        //[ResponseType(typeof(List<CustomProperty>))]
        //public HttpResponseMessage GetListingTypeProperties(string name)
        //{
        //    return Request.CreateResponse(HttpStatusCode.OK, ListingClient.GetListingTypeProperties(name, "Site"));
        //}
    }
}
