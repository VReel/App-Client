package io.vreel.vreel;
 
import com.unity3d.player.UnityPlayerActivity;
import java.io.File;
import java.nio.Buffer;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.media.ThumbnailUtils;
import android.os.Bundle;
import android.os.Environment;
import android.util.Log;

public class JavaPlugin extends UnityPlayerActivity
{
    private static final String TAG = "JavaPlugin";    
    
    @Override
    protected void onCreate(Bundle myBundle) 
    {
        super.onCreate(myBundle);
    }
    
    @Override
    protected void onResume() 
    {    	
    	Log.d(TAG, "onResume");        
        
        super.onResume();
    }
    
    @Override
    protected void onPause()
    {
        super.onPause();
    }
    
    @Override
    protected void onStop() 
    {
    	Log.d(TAG, "onStop");
    	
        super.onStop();
    }    
    
    public static String GetAndroidImagesPath()
    {
    	String path = Environment.getExternalStoragePublicDirectory(
    					Environment.DIRECTORY_DCIM).getAbsolutePath();
    	return path;
    }
    
    public static float CalcAspectRatio(String path)
    {   
		BitmapFactory.Options options = new BitmapFactory.Options();
		options.inJustDecodeBounds = true;
		BitmapFactory.decodeFile(new File(path).getAbsolutePath(), options);
		int imageWidth = options.outWidth;
		int imageHeight = options.outHeight;		

        return imageWidth/ (float) imageHeight; // casting to float in order to ensure float output
    }
    
    public static void CreateThumbnail(String path, Buffer buf)
    {
    	final int kThumbnailWidth = 320;
    	Bitmap thumbnailImage = 
    			ThumbnailUtils.extractThumbnail(BitmapFactory.decodeFile(path), kThumbnailWidth, kThumbnailWidth/2);    	
    	thumbnailImage.copyPixelsToBuffer(buf);
    }
}
