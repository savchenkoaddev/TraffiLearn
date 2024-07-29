﻿using MediatR;
using Microsoft.AspNetCore.Http;
using TraffiLearn.Application.Abstractions.Data;
using TraffiLearn.Application.Abstractions.Storage;
using TraffiLearn.Domain.Entities;
using TraffiLearn.Domain.Errors.Questions;
using TraffiLearn.Domain.Errors.Topics;
using TraffiLearn.Domain.RepositoryContracts;
using TraffiLearn.Domain.Shared;
using TraffiLearn.Domain.ValueObjects;

namespace TraffiLearn.Application.Commands.Questions.Update
{
    public sealed class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, Result>
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IBlobService _blobService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateQuestionCommandHandler(
            IQuestionRepository questionRepository,
            ITopicRepository topicRepository,
            IBlobService blobService,
            IUnitOfWork unitOfWork)
        {
            _questionRepository = questionRepository;
            _topicRepository = topicRepository;
            _blobService = blobService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
        {
            var question = await _questionRepository.GetByIdAsync(
                request.QuestionId.Value,
                includeExpression: x => x.Topics);

            if (question is null)
            {
                return QuestionErrors.NotFound;
            }

            var answers = request.Answers.Select(x => Answer.Create(x.Text, x.IsCorrect.Value)).ToList();

            question.Update(
                content: QuestionContent.Create(request.Content),
                explanation: QuestionExplanation.Create(request.Explanation),
                ticketNumber: TicketNumber.Create(request.TicketNumber.Value),
                questionNumber: QuestionNumber.Create(request.QuestionNumber.Value),
                answers: answers,
                imageUri: question.ImageUri);

            var updateTopicsResult = await UpdateTopics(
                topicsIds: request.TopicsIds,
                question: question);

            if (updateTopicsResult.IsFailure)
            {
                return updateTopicsResult.Error;
            }

            await _questionRepository.UpdateAsync(question);

            var imageUpdateResult = await HandleImageAsync(
                image: request.Image,
                question,
                request.RemoveOldImageIfNewImageMissing.Value,
                cancellationToken);

            if (imageUpdateResult.IsFailure)
            {
                return imageUpdateResult.Error;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        private async Task<Result> UpdateTopics(
            List<Guid?>? topicsIds,
            Question question)
        {
            foreach (var topicId in topicsIds)
            {
                var topic = await _topicRepository.GetByIdAsync(topicId.Value);

                if (topic is null)
                {
                    return TopicErrors.NotFound;
                }

                if (!question.Topics.Contains(topic))
                {
                    question.AddTopic(topic);
                }
            }

            var questionTopics = question.Topics.ToList();

            foreach (var topic in questionTopics)
            {
                if (!topicsIds.Contains(topic.Id))
                {
                    question.RemoveTopic(topic);
                }
            }

            return Result.Success();
        }

        private async Task<Result> HandleImageAsync(
            IFormFile? image,
            Question question,
            bool removeOldImageIfNewImageMissing,
            CancellationToken cancellationToken)
        {
            if (image is null)
            {
                if (question.ImageUri is not null && removeOldImageIfNewImageMissing)
                {
                    await _blobService.DeleteAsync(
                        blobUri: question.ImageUri.Value,
                        cancellationToken);

                    question.SetImageUri(null);
                }
            }
            else
            {
                if (question.ImageUri is not null)
                {
                    await _blobService.DeleteAsync(
                        blobUri: question.ImageUri.Value,
                        cancellationToken);
                }

                using Stream stream = image.OpenReadStream();

                var uploadResponse = await _blobService.UploadAsync(
                    stream,
                    image.ContentType,
                    cancellationToken);

                var imageUriResult = ImageUri.Create(uploadResponse.BlobUri);

                if (imageUriResult.IsFailure)
                {
                    return Result.Failure(Error.InternalFailure());
                }

                question.SetImageUri(imageUriResult.Value);
            }

            return Result.Success();
        }
    }
}
