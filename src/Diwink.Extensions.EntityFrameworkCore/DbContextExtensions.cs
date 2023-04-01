using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;


namespace Diwink.Extensions.EntityFrameworkCore
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Get list of objects that represents the Primary Key of an entity
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static object[] GetPrimaryKeyValues(this EntityEntry entry)
        {
            return entry.Metadata.FindPrimaryKey()
                .Properties
                .Select(p => entry.Property(p.Name).CurrentValue)
                .ToArray();
        }

        /// <summary>
        /// simple update method that will help you to do a full update to an aggregate graph with all related entities in it.
        /// the update method will take the loaded aggregate entity from the DB and the passed one that may come from the API layer.
        /// the method will update just the eager loaded entities in the aggregate "The included entities"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="newEntity">The De-Attached Entity</param>
        /// <param name="existingEntity">The Attached BD Entity</param>
        public static T InsertUpdateOrDeleteGraph<T>(this DbContext context, T newEntity, T existingEntity) where T : class
        {
            return insertUpdateOrDeleteGraph(context, newEntity, existingEntity, null);
        }


        private static T insertUpdateOrDeleteGraph<T>(this DbContext context, T newEntity, T existingEntity, string aggregateType) where T : class
        {
            if (existingEntity == null)
            {
                context.Add(newEntity);
                return newEntity;
            }
            else if (newEntity == null)
            {
                context.Remove(existingEntity);
                return null;
            }
            else
            {
                var existingEntry = context.Entry(existingEntity);
                existingEntry.CurrentValues.SetValues(newEntity);

                foreach (var navigationEntry in existingEntry.Navigations.Where(n => n.IsLoaded && n.Metadata.ClrType.FullName != aggregateType))
                {
                    var passedNavigationObject = existingEntry.Entity.GetType().GetProperty(navigationEntry.Metadata.Name)?.GetValue(newEntity);

                    //if (navigationEntry.Metadata.IsCollection()) causes Error CS1929  'INavigationBase' does not contain a definition for 'IsCollection' and the best extension method overload 'NavigationExtensions.IsCollection(INavigation)' requires a receiver of type 'INavigation'   
                    //use instead https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.collectionentry?view=efcore-7.0
                    if (navigationEntry is CollectionEntry)
                    {
                        // the navigation property is list
                        if (!(navigationEntry.CurrentValue is IEnumerable<object> existingNavigationObject))
                            throw new NullReferenceException($"Couldn't iterate through the DB value of the Navigation '{navigationEntry.Metadata.Name}'");
                        IEnumerable<object> passedNavigationObjectEnumerable = null;

                        if (passedNavigationObject != null) //skip null children  in newEntity. 
                        {
                            passedNavigationObjectEnumerable = passedNavigationObject as IEnumerable<object>;
                            if (passedNavigationObject ==null)
                                throw new NullReferenceException($"Couldn't iterate through the passed Navigation list of '{navigationEntry.Metadata.Name}'");

                            foreach (var newValue in passedNavigationObjectEnumerable)
                            {
                                var newId = context.Entry(newValue).GetPrimaryKeyValues();
                                var existingValue = existingNavigationObject.FirstOrDefault(v => context.Entry(v).GetPrimaryKeyValues().SequenceEqual(newId));
                                if (existingValue == null)
                                {
                                    var addMethod = existingNavigationObject.GetType().GetMethod("Add");

                                    if (addMethod == null)
                                        throw new NullReferenceException($"The collection type in the Navigation property '{navigationEntry.Metadata.Name}' doesn't have an 'Add' method.");

                                    addMethod.Invoke(existingNavigationObject, new[] { newValue });
                                }

                                //Update sub navigation
                                insertUpdateOrDeleteGraph(context, newValue, existingValue, existingEntry.Metadata.ClrType.FullName);
                            }
                        }

                        foreach (var existingValue in existingNavigationObject.ToList())
                        {
                            var existingId = context.Entry(existingValue).GetPrimaryKeyValues();
							//If passedNavigationObject is null, delete existing records
                            if (passedNavigationObject==null || passedNavigationObjectEnumerable.All(v => !context.Entry(v).GetPrimaryKeyValues().SequenceEqual(existingId)))
                            {
                                var removeMethod = existingNavigationObject.GetType().GetMethod("Remove");

                                if (removeMethod == null)
                                    throw new NullReferenceException($"The collection type in the Navigation property '{navigationEntry.Metadata.Name}' doesn't have a 'Remove' method.");
                                
                                removeMethod.Invoke(existingNavigationObject, new[] { existingValue });
                            }
                        }
                    }
                    else
                    {
                        // the navigation is not a list
                        insertUpdateOrDeleteGraph(context, passedNavigationObject, navigationEntry.CurrentValue, existingEntry.Metadata.ClrType.FullName);
                    }
                }

                return existingEntity;
            }
        }
    }
}
