/*  Copyright (C) 2008-2017 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy 
 *  of this software and associated documentation files (the "Software"), to deal 
 *  in the Software without restriction, including without limitation the rights 
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 *  copies of the Software, and to permit persons to whom the Software is 
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 *  THE SOFTWARE. 
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Device
   {
      /// <summary>[AlphaFS] Enumerates the volumes and associated physical drives on the Computer.</summary>
      /// <returns>An IEnumerable of type <see cref="PhysicalDriveInfo"/> that represents the physical drives on the Computer.</returns>      
      [SecurityCritical]
      public static IEnumerable<PhysicalDriveInfo> EnumerateVolumes()
      {
         var physicalDrives = EnumerateDevicesCore(null, DeviceGuid.Disk, false).Select(deviceInfo => GetPhysicalDriveInfoCore(null, deviceInfo)).Where(physicalDrive => null != physicalDrive).OrderBy(disk => disk.DeviceNumber).ToArray();

         var volumeGuids = Volume.EnumerateVolumes().ToArray();


         var populatedPhysicalDrives = new Collection<PhysicalDriveInfo>();


         foreach (var volume in volumeGuids)
         {
            var pDriveInfo = GetPhysicalDriveInfoCore(volume, null, false);

            if (null == pDriveInfo)
               continue;


            foreach (var pDrive in physicalDrives.Where(pDrive => pDrive.DeviceNumber == pDriveInfo.DeviceNumber))
            {
               CopyTo(pDrive, pDriveInfo);


               foreach (var lDrive in Volume.EnumerateVolumePathNames(volume).Where(drive => !Utils.IsNullOrWhiteSpace(drive) && Path.IsLogicalDriveCore(drive, PathFormat.LongFullPath)))
               {
                  if (null == pDriveInfo.DriveInfo)
                     pDriveInfo.DriveInfo = new Collection<DriveInfo>();

                  pDriveInfo.DriveInfo.Add(new DriveInfo(lDrive));
               }


               pDriveInfo.VolumeGuids = new[] {volume};

               populatedPhysicalDrives.Add(pDriveInfo);

               break;
            }
         }


         return populatedPhysicalDrives.OrderBy(pDriveInfo => pDriveInfo.DeviceNumber).ThenBy(pDriveInfo => pDriveInfo.PartitionNumber);
      }
   }
}