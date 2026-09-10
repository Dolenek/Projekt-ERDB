"""Binomial summaries for replay outcomes; rejections count as unsuccessful recognition."""
import collections
import math


def wilson(successes, trials):
    if trials <= 0 or not 0 <= successes <= trials:
        raise ValueError('Require 0 <= successes <= trials and trials > 0')
    critical = 1.959963984540054
    proportion = successes / trials
    denominator = 1 + critical ** 2 / trials
    center = (proportion + critical ** 2 / (2 * trials)) / denominator
    radius = critical * math.sqrt(proportion * (1 - proportion) / trials + critical ** 2 / (4 * trials ** 2)) / denominator
    return dict(Successes=successes, Trials=trials, Estimate=proportion, Lower=max(0, center - radius),
                Upper=min(1, center + radius), ConfidenceLevel=.95,
                Method='Wilson score, two-sided, without continuity correction')


def totals(predictions):
    correct = sum(prediction['Correct'] for prediction in predictions)
    return dict(Total=len(predictions), Correct=correct,
                Wrong=sum(prediction['Accepted'] and not prediction['Correct'] for prediction in predictions),
                Rejected=sum(not prediction['Accepted'] for prediction in predictions), Accuracy95=wilson(correct, len(predictions)))


def summary(predictions):
    result = totals(predictions)
    for name, key in (('ByItem', lambda prediction: prediction['Expected']),
                      ('ByCondition', lambda prediction: ('grayscale' if prediction['Grayscale'] else 'color') +
                       ('/lines' if prediction['Lines'] else '/no-lines'))):
        groups = collections.defaultdict(list)
        for prediction in predictions:
            groups[key(prediction)].append(prediction)
        result[name] = [dict(Group=label, **totals(group)) for label, group in sorted(groups.items())]
    return result
