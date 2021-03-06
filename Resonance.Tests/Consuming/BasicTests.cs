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
    public class BasicTests
    {
        private readonly IEventPublisher _publisher;
        private readonly IEventConsumer _consumer;

        public BasicTests(EventingRepoFactoryFixture fixture)
        {
            _publisher = new EventPublisher(fixture.RepoFactory);
            _consumer = new EventConsumer(fixture.RepoFactory);
        }

        [Fact]
        public void VisibilityTimeout()
        {
            // Arrange
            var topicName = "Consuming.BasicTests.VisibilityTimeout";
            var subName = topicName + "_Sub1";
            var topic = _publisher.AddOrUpdateTopicAsync(new Topic { Name = topicName }).Result;
            var sub1 = _consumer.AddOrUpdateSubscriptionAsync(new Subscription
            {
                Name = subName,
                TopicSubscriptions = new List<TopicSubscription> { new TopicSubscription { TopicId = topic.Id.Value, Enabled = true } },
            }).Result;

            _publisher.Publish(topicName);

            var visibilityTimeout = 2;
            var ce1 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: visibilityTimeout).Result.SingleOrDefault();
            Assert.NotNull(ce1);
            var ce2 = _consumer.ConsumeNextAsync(subName).Result.SingleOrDefault();
            Assert.Null(ce2); // Locked, so should not be returned.

            Thread.Sleep(TimeSpan.FromSeconds(visibilityTimeout+1)); // Wait until visibilitytimeout has expired
            ce2 = _consumer.ConsumeNextAsync(subName).Result.SingleOrDefault();
            Assert.NotNull(ce2); // Should be unlocked again
        }

        [Fact]
        public void DeliveryDelay()
        {
            // Arrange
            var topicName = "Consuming.BasicTests.DeliveryDelay";
            var subName = topicName + "_Sub1";
            var topic = _publisher.AddOrUpdateTopicAsync(new Topic { Name = topicName }).Result;
            var deliveryDelay = 2;
            var sub1 = _consumer.AddOrUpdateSubscriptionAsync(new Subscription
            {
                Name = subName, DeliveryDelay = deliveryDelay,
                TopicSubscriptions = new List<TopicSubscription> { new TopicSubscription { TopicId = topic.Id.Value, Enabled = true } },
            }).Result;

            _publisher.PublishAsync(topicName).Wait();

            var ce1 = _consumer.ConsumeNextAsync(subName).Result.SingleOrDefault();
            Assert.Null(ce1); // Should not yet be delivered

            Thread.Sleep(TimeSpan.FromSeconds(deliveryDelay)); // Wait until deliverydelay has expired
            var ce2 = _consumer.ConsumeNextAsync(subName).Result.SingleOrDefault();
            Assert.NotNull(ce2); // Should be unlocked again
        }

        [Fact]
        public void MaxDeliveries()
        {
            // Arrange
            var topicName = "Consuming.BasicTests.MaxDeliveries";
            var subName = topicName + "_Sub1";
            var topic = _publisher.AddOrUpdateTopicAsync(new Topic { Name = topicName }).Result;
            var maxDeliveries = 2;
            var sub1 = _consumer.AddOrUpdateSubscriptionAsync(new Subscription
            {
                Name = subName,
                MaxDeliveries = maxDeliveries,
                TopicSubscriptions = new List<TopicSubscription> { new TopicSubscription { TopicId = topic.Id.Value, Enabled = true } },
            }).Result;

            _publisher.PublishAsync(topicName).Wait();

            for (int i = 0; i < maxDeliveries; i++)
            {
                var ce1 = _consumer.ConsumeNextAsync(subName, visibilityTimeout: 1).Result.SingleOrDefault();
                Assert.NotNull(ce1); // Should succeed
                Thread.Sleep(TimeSpan.FromSeconds(1)); // Wait until visibility timeout has expired
            }

            var ce2 = _consumer.ConsumeNextAsync(subName).Result.SingleOrDefault();
            Assert.Null(ce2); // Maxdeliveries is reached. Should not be delivered anymore
        }

        [Fact]
        public void TimeToLive()
        {
            // Arrange
            var topicName = "Consuming.BasicTests.TimeToLive";
            var subName = topicName + "_Sub1";
            var topic = _publisher.AddOrUpdateTopicAsync(new Topic { Name = topicName }).Result;
            var ttl = 1;
            var sub1 = _consumer.AddOrUpdateSubscriptionAsync(new Subscription
            {
                Name = subName, TimeToLive = ttl,
                TopicSubscriptions = new List<TopicSubscription> { new TopicSubscription { TopicId = topic.Id.Value, Enabled = true } },
            }).Result;

            _publisher.PublishAsync(topicName).Wait();
            var ce = _consumer.ConsumeNextAsync(subName, visibilityTimeout: 1).Result.SingleOrDefault();
            Assert.NotNull(ce);
            Thread.Sleep(TimeSpan.FromSeconds(1)); // Wait until visibility timeout has expired
            ce = _consumer.ConsumeNextAsync(subName).Result.SingleOrDefault();
            Assert.Null(ce); // Time to live has passed, so should not be delivered anymore
        }

        [Fact]
        public void TimeToLiveCapped()
        {
            // Arrange
            var topicName = "Consuming.BasicTests.TimeToLiveCapped";
            var subName = topicName + "_Sub1";
            var topic = _publisher.AddOrUpdateTopicAsync(new Topic { Name = topicName }).Result;
            var ttl = 3;
            var sub1 = _consumer.AddOrUpdateSubscriptionAsync(new Subscription
            {
                Name = subName,
                TimeToLive = ttl,
                TopicSubscriptions = new List<TopicSubscription> { new TopicSubscription { TopicId = topic.Id.Value, Enabled = true } },
            }).Result;

            _publisher.PublishAsync(topicName, expirationDateUtc: DateTime.UtcNow.AddSeconds(1)).Wait();
            var ce = _consumer.ConsumeNextAsync(subName, visibilityTimeout: 1).Result.SingleOrDefault();
            Assert.NotNull(ce);
            Thread.Sleep(TimeSpan.FromSeconds(1)); // Wait until visibility timeout has expired
            ce = _consumer.ConsumeNextAsync(subName).Result.SingleOrDefault();
            Assert.Null(ce); // Time to live has NOT YET passed, but expirationdate HAS so should not be delivered anymore
        }
    }
}
