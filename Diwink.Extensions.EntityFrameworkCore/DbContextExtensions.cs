using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;


namespace Diwink.Extensions.EntityFrameworkCore
{
    public static class DbContextExtensions
    {
        public static object[] GetPrimaryKeyValues(this EntityEntry entry)
        {
            return entry.Metadata.FindPrimaryKey()
                .Properties
                .Select(p => entry.Property(p.Name).CurrentValue)
                .ToArray();
        }


        public static void InsertUpdateOrDeleteGraph<T>(this DbContext context, T newEntity, T existingEntity) where T : class
        {
            if (newEntity == null)
                throw new ArgumentNullException(nameof(newEntity), "The new value cannot be null");

            if (existingEntity == null)
            {
                context.Add(newEntity);
            }
            else
            {
                var existingEntry = context.Entry(existingEntity);
                existingEntry.CurrentValues.SetValues(newEntity);

                foreach (var navigationEntry in existingEntry.Navigations.Where(n => n.IsLoaded))
                {
                    var x1 = navigationEntry.Metadata.IsDependentToPrincipal();
                    var x2 = navigationEntry.Metadata.ForeignKey.DependentToPrincipal;
                    var x3 = navigationEntry.Metadata.IsShadowProperty();
                    var x4 = navigationEntry.Metadata.DeclaringEntityType;

                    var passedNavigationObject = existingEntry.Entity.GetType().GetProperty(navigationEntry.Metadata.Name)?.GetValue(newEntity);

                    if (passedNavigationObject == null)
                        continue;

                    if (navigationEntry.Metadata.IsCollection())
                    {
                        // the navigation property is list
                        if (!(navigationEntry.CurrentValue is IEnumerable<object> existingNavigationObject))
                            throw new NullReferenceException($"Couldn't iterate through the DB value of the Navigation '{navigationEntry.Metadata.Name}'");

                        if (!(passedNavigationObject is IEnumerable<object> passedNavigationObjectEnumerable))
                            throw new NullReferenceException($"Couldn't iterate through the passed Navigation list of '{navigationEntry.Metadata.Name}'");

                        var existingNavigationObjectList = existingNavigationObject;
                        var passedNavigationObjectList = passedNavigationObjectEnumerable;

                        foreach (var newValue in passedNavigationObjectList)
                        {
                            var newId = context.Entry(newValue).GetPrimaryKeyValues();
                            var existingValue = existingNavigationObjectList.FirstOrDefault(v => context.Entry(v).GetPrimaryKeyValues().SequenceEqual(newId));
                            if (existingValue == null)
                            {
                                var addMethod = existingNavigationObjectList.GetType().GetMethod("Add");

                                if (addMethod == null)
                                    throw new NullReferenceException($"The collection type in the Navigation property '{navigationEntry.Metadata.Name}' doesn't have an 'Add' method.");

                                addMethod.Invoke(existingNavigationObjectList, new[] {newValue});
                            }

                            //Update sub navigation
                            InsertUpdateOrDeleteGraph(context, newValue, existingValue);
                        }

                        foreach (var existingValue in existingNavigationObjectList.ToList())
                        {
                            var existingId = context.Entry(existingValue).GetPrimaryKeyValues();

                            if (passedNavigationObjectList.All(v => !context.Entry(v).GetPrimaryKeyValues().SequenceEqual(existingId)))
                            {
                                context.Remove(existingValue);
                            }
                        }
                    }
                    else
                    {
                        // the navigation is not a list
                        InsertUpdateOrDeleteGraph(context, passedNavigationObject, navigationEntry.CurrentValue);
                    }
                }
            }
        }
    }
}
