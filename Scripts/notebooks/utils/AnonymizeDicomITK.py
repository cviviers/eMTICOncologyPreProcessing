import argparse
import itk
import sys
import os

# Create new directory
def makedirs(path):
    if not os.path.exists( path ):
        os.makedirs( path )
        print(f"Created output directory: {path}")

def recursive_search( input_dir, output_dir, tags ):
    """ finds subfolders """

    print("Starting recursive process..")
    for subdir, _, _ in os.walk(r"{}".format(input_dir)):
        output_path = os.path.join(output_dir, subdir.replace(input_dir, "")[1:])
        anonymize_dicom(subdir, output_path, tags)
            

def anonymize_dicom(data_path, output_path, tags):
    
    PixelType = itk.ctype('signed short')
    Dimension = 3
    ImageType = itk.Image[PixelType, Dimension]

    namesGenerator = itk.GDCMSeriesFileNames.New()
    namesGenerator.SetUseSeriesDetails(False)
    namesGenerator.AddSeriesRestriction("0008|0021")
    namesGenerator.SetGlobalWarningDisplay(False)
    namesGenerator.SetDirectory(data_path)

    seriesUID = namesGenerator.GetSeriesUIDs()

    if not seriesUID:
        print("The given directory \""+data_path+"\" does NOT contain a DICOM series.\n")
        return
    else:
        print("The given directory \""+data_path+"\" does contain a DICOM series.")

    seriesFound = False
    for uid in seriesUID:
        seriesIdentifier = uid

        print('Reading: ' + seriesIdentifier)

        fileNames = namesGenerator.GetFileNames(seriesIdentifier)

        reader = itk.ImageSeriesReader[ImageType].New()
        dicomIO = itk.GDCMImageIO.New()
        reader.SetImageIO(dicomIO)
        reader.SetFileNames(fileNames)
        reader.ForceOrthogonalDirectionOff()
        reader.Update()


        # reader_header = dicomIO.GetMetaDataDictionary()
        # dicom_header = itk.MetaDataObject()

        # start_dict = dictionary.Begin()
        # end_dict = dictionary.End()

        size = itk.size(reader.GetOutput())

        

        metadata = reader.GetMetaDataDictionary()
        print('metadata: ', metadata)

        itk.EncapsulateMetaData()

        data_writer = itk.ImageFileWriter[ImageType].New()
        outFileName = os.path.join(output_image, series_name + '_0000.nii.gz')
        data_writer.SetFileName(outFileName)
        data_writer.UseCompressionOn()
        #data_writer.UseInputMetaDataDictionaryOn ()
        data_writer.SetInput(reader.GetOutput())
        

        print('Writing: ' + outFileName)
        
        data_writer.Update()

        if seriesFound:
            break
    

    # print("Starting anonymizing process..")
    # makedirs(output_path)

    # for i, series_ID in enumerate(series_IDs):
        
    #     print(f"Starting with series: {i}, name: {series_ID}")
    #     series_file_names = sitk.ImageSeriesReader.GetGDCMSeriesFileNames(data_path, series_ID, useSeriesDetails=False) #useSeriesDetails ?
    #     series_reader = sitk.ImageSeriesReader()
        
    #     series_reader.SetFileNames(series_file_names)
        
    #     series_reader.MetaDataDictionaryArrayUpdateOn()
    #     series_reader.LoadPrivateTagsOn()

    #     # load Dicom series
    #     try:
    #         imgs = series_reader.Execute()

    #     except RuntimeError:
    #         print ("--> Fundamental error in image layer, skipping...")
    #         continue

    #     # step through each slice in the series and check header/metadata
    #     for i, image_name in enumerate(series_file_names):
    #         nreader = sitk.ReadImage(image_name, imageIO="GDCMImageIO")

    #         writer = sitk.ImageFileWriter()
    #         writer.KeepOriginalImageUIDOn()
        
    #         # Replace tags with empty strings ie. remove value of tag
    #         image_slice = nreader
    #         for tag in tags:
    #             image_slice.SetMetaData(tag, "")

    #         # image_slice.SetMetaData("0020|0013", str(i))

    #         writer.SetFileName(os.path.join(output_path, image_name.split("/")[1]+'.dcm' ))
    #         writer.Execute(image_slice)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(prog='AnonymizeDicom')
    parser.add_argument('--input', type=str, help='directory containing dicom series')
    parser.add_argument('--output', type=str, help='directory to store anonymized dicoms')
    parser.add_argument('--tags', nargs='+', type=str, default=["0008|0012", "0008|0013"], help='[Date, Time] list of tags to remove from series')
    parser.add_argument('--recursive', dest='recursive', default=False, action='store_true', help='recursively search subdirectories for dicom series')
    
    opt = parser.parse_args()

    if opt.recursive==True:
        recursive_search(opt.input, opt.output, opt.tags)
    else:
        anonymize_dicom(opt.input, opt.output, opt.tags)



