clear all
% close all
clc
% If false, show the figures as subplots
createSeperateFigures = false;

% Which figures to plot
plotCorrectPercents = false;
plotCorrectOverTime = false;
plotAverageTimePerStiffness = false;
plotConflictPercents = true;

% Maximum number of plots to show side by side if createSeperateFigures is false
maxPlotPerColumn = 4;

%Get the experiment file name
[filenames, pathname] = uigetfile('*.csv', 'Select an experiment file', 'MultiSelect', 'on');

if isequal(filenames, 0) || isequal(pathname, 0)
    disp('File not selected')
    return
else
     if iscell(filenames)
        disp(['Selected ', num2str(length(filenames)), ' files'])
        for i = 1:length(filenames)
            disp(['File ', num2str(i), ': ', filenames{i}])
        end
     else
        disp(['Selected file: ', fullfile(pathname, filenames)])
    end
end

if iscell(filenames)
    numFiles = length(filenames);
else
    numFiles = 1;
end
%%
for file = 1:numFiles
    if numFiles == 1
        filename = filenames;
    else
        filename = filenames{file};
    end

    data = readmatrix(fullfile(pathname, filename), 'NumHeaderLines', 0);
    experimentDate = split(filename, '_');
    subjectName = experimentDate{1};
    isVisualConflicted = contains(filename, ["Conflict", "conflict"]);
    % isVisualConflicted = false;

    timeDataAvailable = size(data, 2) >= 4;
    totalTrials = size(data, 1);
    referenceStiffness = mode(data(:, 1:2), 'all');

    adjustedStiffnessValues = [];

    for i = 1:totalTrials

        if data(i, 1) ~= referenceStiffness
            adjustedStiffnessValues = [adjustedStiffnessValues; data(i, 1)];
        else
            adjustedStiffnessValues = [adjustedStiffnessValues; data(i, 2)];
        end

    end

    percentDeltaStiffness = (adjustedStiffnessValues - referenceStiffness) / referenceStiffness * 100;

    uniqueDeltas = unique(percentDeltaStiffness);
    numberOfDeltas = length(uniqueDeltas);
    trialPerCondition = totalTrials / numberOfDeltas;

    correctIndices = [];
    wrongIndices = [];

    % First column is the value of stiffness in percent
    % Second column is the index of correct answers
    % Third column is the index of wrong answers
    % Fourth column is the correct percentage
    % Fifth column is the time it took to answer
    % Sixth column is the average of fifth
    results = num2cell(uniqueDeltas);

    for i = 1:numberOfDeltas
        results{i, 2} = [];
        results{i, 3} = [];
        results{i, 4} = 0;
        results{i, 5} = [];
        results{i, 6} = [];
    end

    for i = 1:totalTrials
        answer = data(i, 3);
        stiffness1 = data(i, 1);
        stiffness2 = data(i, 2);
        percent = percentDeltaStiffness(i);
        deltaIndex = find([results{:, 1}] == percent);

        if timeDataAvailable
            timeItTookToAnswer = data(i, 4);

            if answer ~= -1
                results{deltaIndex, 5} = [results{deltaIndex, 5} timeItTookToAnswer];
            end

            results{deltaIndex, 6} = mean(results{deltaIndex, 5});
        end

        if answer == 0

            if stiffness1 > stiffness2
                results{deltaIndex, 2} = [results{deltaIndex, 2} i];
            else
                results{deltaIndex, 3} = [results{deltaIndex, 3} i];
            end

        elseif answer == 1

            if stiffness2 >= stiffness1
                results{deltaIndex, 2} = [results{deltaIndex, 2} i];
            else
                results{deltaIndex, 3} = [results{deltaIndex, 3} i];
            end

        end

        results{deltaIndex, 4} = length(results{deltaIndex, 2}) / trialPerCondition * 100;
    end

    if (plotCorrectPercents)

        if ~createSeperateFigures
            figure(1)
            subplot(ceil(numFiles / maxPlotPerColumn), min(numFiles, maxPlotPerColumn), file)
        else
            figure
        end

        bar([results{:, 1}], [results{:, 4}])
        ylim([0 100])
        xlim([min([results{:, 1}]) - 5, max([results{:, 1}]) + 5])
        ylabel('Percent Correct (%)')
        xlabel('Percent Change in Stiffness (%)')
        title([{['Subject: ', subjectName], experimentDate{2}}])
    end

    % Calculate correct percentage over time
    correctPercentOverTime = [];
    correctCount = 0;

    for i = 1:totalTrials
        correctCount = length(find([results{:, 2}] < i));
        correctPercentOverTime = [correctPercentOverTime; correctCount / i];
    end

    if plotCorrectOverTime

        if ~createSeperateFigures
            figure(2)
            subplot(ceil(numFiles / maxPlotPerColumn), min(numFiles, maxPlotPerColumn), file)
        else
            figure
        end

        plot(1:totalTrials, correctPercentOverTime);
        xlabel('Trial');
        ylabel('Correct Percentage');
        title({'Correct Percent Overtime', ['Subject: ', subjectName], experimentDate{2}});
    end

    % Plot the time it took the answer each stiffness value
    if (timeDataAvailable && plotAverageTimePerStiffness)

        if ~createSeperateFigures
            figure(3)
            subplot(ceil(numFiles / maxPlotPerColumn), min(numFiles, maxPlotPerColumn), file)
        else
            figure
        end

        bar([results{:, 1}], [results{:, 6}])
        ylim([0 30])
        xlim([min([results{:, 1}]) - 5 max([results{:, 1}]) + 5])
        ylabel('Time (s)')
        xlabel('Percent Change in Stiffness (%)')
        title([{"Average Time To Answer a Value", ['Subject: ', subjectName], experimentDate{2}}])
    end
    fprintf("Average Answering Time (%s): %.2f\n", subjectName, mean([results{:, 6}]))

    %%
    % I hate myself for implementing this like this
    if isVisualConflicted
        % Count the number of unique lambda values
        uniqueLambdaValues = unique(data(:, 5));
        numberOfLambdas = length(uniqueLambdaValues);

        % First column is the value of stiffness in percent
        % Second column is the index of correct answers
        % Third column is the index of wrong answers
        % Fourth column is the correct percentage
        % Fifth column is the time it took to answer
        % Sixth column is the average of fifth

        % Create a cell array for each lambda value
        for i = 1:numberOfLambdas
            lambda = uniqueLambdaValues(i);
            eval(['resultsLambda' num2str(lambda * 100) ' = num2cell(uniqueDeltas);']);

            for i = 1:numberOfDeltas
                eval(['resultsLambda' num2str(lambda * 100) '{i, 2} = [];']);
                eval(['resultsLambda' num2str(lambda * 100) '{i, 3} = [];']);
                eval(['resultsLambda' num2str(lambda * 100) '{i, 4} = 0;']);
                eval(['resultsLambda' num2str(lambda * 100) '{i, 5} = [];']);
                eval(['resultsLambda' num2str(lambda * 100) '{i, 6} = [];']);
            end

        end

        for i = 1:totalTrials
            i;
            answer = data(i, 3);
            stiffness1 = data(i, 1);
            stiffness2 = data(i, 2);
            percent = percentDeltaStiffness(i);
            lambda = data(i, 5);
            % deltaIndex = find([eval(['resultsLambda' num2str(lambda * 100) '{:,1}'])] == percent]);
            deltaIndex = eval(['find([resultsLambda' num2str(lambda * 100) '{:,1}] == percent);']);

            if timeDataAvailable
                timeItTookToAnswer = data(i, 4);

                if answer ~= -1
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 5} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 5} timeItTookToAnswer];']);
                end

                eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 6} = mean(resultsLambda' num2str(lambda * 100) '{deltaIndex, 5});']);
            end

            if answer == 0

                if stiffness1 > stiffness2
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 2} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 2} i];']);
                else
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 3} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 3} i];']);
                end

            elseif answer == 1

                if stiffness2 >= stiffness1
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 2} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 2} i];']);
                else
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 3} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 3} i];']);
                end

            end

            trialCount = length(eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 2}'])) + length(eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 3}']));
            eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 4} = length(resultsLambda' num2str(lambda * 100) '{deltaIndex, 2}) / trialCount * 100;']);
        end

        % Plot the correct percentage of lambda values on the same bar graph
        if plotConflictPercents
            if (~createSeperateFigures)
                figure(4)
                subplot(ceil(numFiles / maxPlotPerColumn), min(numFiles, maxPlotPerColumn), file)
            else
                figure
            end

            x = uniqueLambdaValues * 100;
            y = [];

            for i = 1:numberOfLambdas
                lambda = uniqueLambdaValues(i);
                lambdaResults = [];

                for j = 1:numberOfDeltas
                    lambdaResults = [lambdaResults eval(['resultsLambda' num2str(lambda * 100) '{j, 4}'])];
                end

                y = [y; lambdaResults];
            end

            bar(x, y)
            title([{['Subject: ', subjectName], experimentDate{2}}])
            if createSeperateFigures
                legendPos = 'northeast';
            else
                legendPos = 'southwest';
            end
            legend(["\Delta K = 0.25K_{0}" "\Delta K = 0.50K_{0}" "\Delta K = 0.75K_{0}" "\Delta K = 1.00K_{0}"], 'Location', legendPos)
            xlabel('Percent Conflict (\lambda*100%)')
            ylabel('Percent Correct (%)')
            ylim([0 100])
            % xlim([min(x) - 5 max(x) + 5])
        end

    end
end
