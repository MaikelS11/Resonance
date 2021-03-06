﻿using Resonance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Resonance.Tests.Consuming
{
    [Collection("EventingRepo")]
    public class FunctionalOrderingTests
    {
        private readonly IEventPublisher _publisher;
        private readonly IEventConsumer _consumer;

        public FunctionalOrderingTests(EventingRepoFactoryFixture fixture)
        {
            _publisher = new EventPublisher(fixture.RepoFactory);
            _consumer = new EventConsumer(fixture.RepoFactory);
        }

        [Fact]
        public void SerialDelivery()
        {
            // Arrange
            var topicName = "FunctionalOrderingTests.SerialDelivery";
            var subName = topicName + "_Sub1"; // Substring to prevent too long sub-names
            var topic = _publisher.AddOrUpdateTopicAsync(new Topic { Name = topicName }).Result;
            var sub1 = _consumer.AddOrUpdateSubscriptionAsync(new Subscription
            {
                Name = subName,
                Ordered = true,
                MaxDeliveries = 2,
                TopicSubscriptions = new List<TopicSubscription> { new TopicSubscription { TopicId = topic.Id.Value, Enabled = true } },
            }).Result;

            var publishedDateUtcBaseLine = DateTime.UtcNow.AddSeconds(-60); // Explicitly setting publicationdates to make sure none are the same!
            _publisher.PublishAsync(topicName, payload: "1", functionalKey: "f1", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(1)).Wait();
            _publisher.PublishAsync(topicName, payload: "2", functionalKey: "f1", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(2)).Wait();
            _publisher.PublishAsync(topicName, payload: "3", functionalKey: "f2", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(3)).Wait();
            _publisher.PublishAsync(topicName, payload: "4", functionalKey: "f1", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(4)).Wait();
            _publisher.PublishAsync(topicName, payload: "5", functionalKey: "f2", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(5)).Wait();

            var visibilityTimeout = 5;
            var p1 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault(); // p1 stands for payload "1"
            var p3 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            var p2 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            Assert.Equal("1", p1.Payload);
            Assert.Equal("3", p3.Payload);
            Assert.Null(p2); // p(ayload) 2 has same functional key as p1 and should not be delivered yet

            _consumer.MarkConsumedAsync(p1.Id, p1.DeliveryKey).Wait(); // p1 should be gone, so p2 can be delivered
            p2 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            var p4 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            Assert.Equal("2", p2.Payload);
            Assert.Null(p4); // Again: same functional key as p2, so should not be delivered (and p3 is still locked)
            Thread.Sleep(TimeSpan.FromSeconds(visibilityTimeout + 1)); // Wait until visibilitytimeout of all items has expired

            p2 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            p3 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            p4 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            Assert.Equal("2", p2.Payload); // p2 should be redelivered, since it had expired
            Assert.Equal("3", p3.Payload); // p3 should be redelivered, since it had expired
            Assert.Null(p4); // p4 still has same functional key as p2, so should not be delivered

            _consumer.MarkFailedAsync(p2.Id, p2.DeliveryKey, Reason.Other("test")).Wait();
            p4 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            Assert.Equal("4", p4.Payload);

            Thread.Sleep(TimeSpan.FromSeconds(visibilityTimeout + 1)); // Wait until visibilitytimeout of all items has expired
            p4 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            var p5 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            Assert.Equal("4", p4.Payload); // p4 should be redeliverd (it was only delivered once)
            Assert.Equal("5", p5.Payload); // p3 has reached maxDeliveries, so p5 should now be delivered
        }


        [Fact]
        public void SerialDelivery_WithPriority()
        {
            // Arrange
            var topicName = "SerialDelivery_WithPriority";
            var subName = topicName + "_Sub1"; // Substring to prevent too long sub-names
            var topic = _publisher.AddOrUpdateTopicAsync(new Topic { Name = topicName }).Result;
            var sub1 = _consumer.AddOrUpdateSubscriptionAsync(new Subscription
            {
                Name = subName,
                Ordered = true,
                TopicSubscriptions = new List<TopicSubscription> { new TopicSubscription { TopicId = topic.Id.Value, Enabled = true } },
            }).Result;

            var publishedDateUtcBaseLine = DateTime.UtcNow.AddSeconds(-60); // Explicitly setting publicationdates to make sure none are the same!
            _publisher.PublishAsync(topicName, payload: "1", functionalKey: "f1", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(1)).Wait();
            _publisher.PublishAsync(topicName, payload: "2", functionalKey: "f1", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(2), priority: 10).Wait();
            _publisher.PublishAsync(topicName, payload: "3", functionalKey: "f2", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(3)).Wait();
            _publisher.PublishAsync(topicName, payload: "4", functionalKey: "f1", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(4)).Wait();
            _publisher.PublishAsync(topicName, payload: "5", functionalKey: "f2", publicationDateUtc: publishedDateUtcBaseLine.AddSeconds(5)).Wait();

            var visibilityTimeout = 5;
            var p2 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault(); // p1 stands for payload "1"
            var p3 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            var pNext = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            Assert.Equal("2", p2.Payload); // Higher prio, so comes first
            Assert.Equal("3", p3.Payload);
            Assert.Null(pNext); // No other should be delivered yet

            _consumer.MarkConsumed(p2.Id, p2.DeliveryKey);
            _consumer.MarkConsumed(p3.Id, p3.DeliveryKey);
            var p4 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            var p5 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            pNext = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            Assert.Equal("4", p4.Payload); // p1 should NOT BE delivered: its too old, the higher prio of p2 delivered it first, but also caused p1 to be skipped
            Assert.Equal("5", p5.Payload);
            Assert.Null(pNext); // Nothing more to deliver
        }
    }
}
