import argparse
import os
import numpy as np
import pandas as pd
from sklearn.metrics import precision_recall_fscore_support


def find_column(df, names):
    for n in df.columns:
        if n.lower() in [x.lower() for x in names]:
            return n
    return None


def generate_ratings_for_sheet(df, as_float=False, seed=42):
    np.random.seed(seed)
    working = df.copy()
    # ensure id columns
    user_col = find_column(working, ['UserID', 'UserId', 'user_id', 'user'])
    course_col = find_column(working, ['CourseID', 'CourseId', 'course_id', 'course'])
    if user_col is None or course_col is None:
        raise ValueError('Could not find UserID or CourseID column in sheet')

    # compute popularity / frequency biases
    course_counts = working[course_col].value_counts().astype(float)
    cc_min, cc_max = course_counts.min(), course_counts.max()
    if cc_max - cc_min <= 0:
        norm_counts = course_counts * 0.0
    else:
        norm_counts = (course_counts - cc_min) / (cc_max - cc_min)

    # map back to rows
    working['_course_pop'] = working[course_col].map(norm_counts).fillna(0.0)

    # user activity bias
    user_counts = working[user_col].value_counts().astype(float)
    uc_min, uc_max = user_counts.min(), user_counts.max()
    if uc_max - uc_min <= 0:
        norm_users = user_counts * 0.0
    else:
        norm_users = (user_counts - uc_min) / (uc_max - uc_min)
    working['_user_act'] = working[user_col].map(norm_users).fillna(0.0)

    # base mean rating and effect sizes
    base = 3.2
    course_effect = working['_course_pop'] * 1.2  # up to +1.2
    user_effect = (working['_user_act'] - 0.5) * 0.6  # range roughly [-0.3, +0.3]

    raw_means = base + course_effect + user_effect

    # add per-row noise and scale to 1..5
    noise = np.random.normal(0, 0.6, size=len(working))
    raw = raw_means + noise
    # clip to 1..5
    ratings = np.clip(np.round(raw, 2) if as_float else np.rint(raw), 1, 5)

    if as_float:
        working['Rating'] = ratings.astype(float)
    else:
        working['Rating'] = ratings.astype(int)

    # drop helper cols
    working = working.drop(columns=['_course_pop', '_user_act'])
    return working


def evaluate_with_noisy_prediction(df, rating_col='Rating', noise_std=1.0, threshold=4.0, seed=42):
    np.random.seed(seed + 1)
    if rating_col not in df.columns:
        raise ValueError('rating_col not in dataframe')
    true = df[rating_col].astype(float).to_numpy()
    pred = np.round(np.clip(true + np.random.normal(0, noise_std, size=len(true)), 1.0, 5.0), 2)
    true_rel = (true >= threshold).astype(int)
    pred_rel = (pred >= threshold).astype(int)
    p, r, f1, _ = precision_recall_fscore_support(true_rel, pred_rel, average='binary', zero_division=0)
    return p, r, f1


def main():
    parser = argparse.ArgumentParser(description='Fill/mock Rating in mock_user_interactions sheet and evaluate')
    parser.add_argument('--input', '-i', default='Online_Courses_Full.xlsx', help='Input workbook')
    parser.add_argument('--sheet', default='mock_user_interactions', help='Sheet name with interactions')
    parser.add_argument('--output', '-o', default='Online_Courses_Full_WithImages_mocked_ratings.xlsx', help='Output workbook')
    parser.add_argument('--as-float', action='store_true', help='Create float ratings')
    parser.add_argument('--noise', type=float, default=1.0, help='Std dev of noise for simulated predictions')
    parser.add_argument('--threshold', type=float, default=4.0, help='Threshold for relevant (binary)')
    parser.add_argument('--force', action='store_true', help='Overwrite existing Rating column')
    parser.add_argument('--seed', type=int, default=42, help='Random seed')
    args = parser.parse_args()

    if not os.path.exists(args.input):
        print('Input workbook not found:', args.input)
        return

    try:
        df_sheet = pd.read_excel(args.input, sheet_name=args.sheet, engine='openpyxl')
    except Exception:
        try:
            df_sheet = pd.read_excel(args.input, sheet_name=args.sheet)
        except Exception as e:
            print('Could not read sheet', args.sheet, 'from', args.input, '->', e)
            return

    # create or fill Rating
    if 'Rating' in df_sheet.columns and not args.force:
        print('Rating column already exists in sheet. Use --force to overwrite.')
    else:
        df_sheet = generate_ratings_for_sheet(df_sheet, as_float=args.as_float, seed=args.seed)
        print('Filled Rating for', len(df_sheet), 'rows')

    # evaluate by simulating predictions
    p, r, f1 = evaluate_with_noisy_prediction(df_sheet, rating_col='Rating', noise_std=args.noise, threshold=args.threshold, seed=args.seed)
    print('Evaluation (simulated predictions) with threshold', args.threshold)
    print(f'Precision: {p:.4f}')
    print(f'Recall:    {r:.4f}')
    print(f'F1-score:  {f1:.4f}')

    # save back to workbook (replace sheet)
    try:
        with pd.ExcelWriter(args.output, engine='openpyxl') as writer:
            # try to preserve other sheets by reading workbook first
            try:
                xls = pd.ExcelFile(args.input, engine='openpyxl')
                for s in xls.sheet_names:
                    if s == args.sheet:
                        continue
                    sdf = pd.read_excel(xls, sheet_name=s)
                    sdf.to_excel(writer, sheet_name=s, index=False)
            except Exception:
                pass
            df_sheet.to_excel(writer, sheet_name=args.sheet, index=False)
    except Exception:
        df_sheet.to_excel(args.output, index=False)

    print('Saved workbook with mocked ratings to', args.output)


if __name__ == '__main__':
    main()
