/*
 * Click nbfs://nbhost/SystemFileSystem/Templates/Licenses/license-default.txt to change this license
 * Click nbfs://nbhost/SystemFileSystem/Templates/Classes/Class.java to edit this template
 */
package View;

import java.util.Scanner;

/**
 *
 * @author Nangnth
 */
public class InputData {
    private final Scanner scn;

    public InputData(Scanner scn) {
        this.scn = scn;
    }
      // Nhập 1 số nguyên
    public int inputInteger(String mess, String regex){
        System.out.println(mess);
        while(true){
        String number = scn.nextLine();
        if(number.matches(regex)){
            //3scn.close();
            return Integer.parseInt(number);
        } else
                System.out.println("pls input interger with fomat: "+ regex);
        } 
    
    }
    // nhập 1 chuỗi
     public String inputString(String mess, String regex){
        System.out.println(mess);
        while(true){
        String str = scn.nextLine().trim();
        if(str.matches(regex)&& !str.matches("\\s*")){
            //scn.close();
            return str;
        }    
        else 
         System.out.println("pls input string with fomat: "+ regex);
        }    
    }
   
}
