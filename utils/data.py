import os
import shutil

def seperate_train_test(data_dir, test_set, destination_dir ):

    for case in os.listdir(data_dir):
        data_path = os.path.join(data_dir, case)
        if os.path.exists(data_path):
            print(case[-11:-7])
            print(case[case[-11:-7]] in test_set)
            if case[-11:-7] in test_set:
                print(case[-11:-7])
                shutil.move(data_path, destination_dir)
                #mergefolders(os.path.join(img_directory, case), os.path.join(directory, phase))