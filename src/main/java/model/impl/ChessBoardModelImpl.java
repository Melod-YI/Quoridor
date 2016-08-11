package model.impl;

import model.po.BlockPO;
import model.po.PlayerPO;
import model.po.WallPO;

import java.util.ArrayList;
import java.util.List;

import abstracter.Direction;
import abstracter.WallDirection;
import model.impl.UpdateMessage;
import model.impl.BaseModel;
import model.service.ChessBoardModelService;
import model.service.GameModelService;
import model.state.BlockState;
import model.state.GameResultState;
import model.state.GameState;
import model.state.WallState;
import model.vo.BlockVO;
import model.vo.WallVO;

public class ChessBoardModelImpl extends BaseModel implements ChessBoardModelService{
	private BlockPO[][] blockMatrix;
	private PlayerPO[] playerMatrix;
	private WallPO[][] wallMatrixX;//方向为横向的所有墙
	private WallPO[][] wallMatrixY;//方向为纵向的所有墙
	private GameModelService gameModel;
	private int playerNum;
	public static boolean Server = false;
	public static boolean Client = false;
	private int width;
	private int height;
	private int wallNum;
	@Override
	public boolean initialize(int height,int width,int wallNum,int playerNum) {
		// TODO Auto-generated method stub
		this.height=height;
		this.width=width;
		this.wallNum=wallNum;
		this.playerNum=playerNum;
		blockMatrix = new BlockPO[width][height];
		for(int i=0;i<width;i++){
			for(int j=0;j<height;j++){
				blockMatrix[i][j]=new BlockPO(BlockState.empty,i,j);
			}
		}//实例化网格
		this.playerNum=playerNum;//初始化玩家数
		playerMatrix = new PlayerPO[this.playerNum];
		for(int i=0;i<playerNum;i++){
			playerMatrix[i]=new PlayerPO(i+1,0);
		}//实例化玩家队列
		wallMatrixX = new WallPO[width][height+1];
		for(int i=0;i<width;i++){
			for(int j=0;j<height+1;j++){
				wallMatrixX[i][j]=new WallPO(WallState.black,i,j,WallDirection.horizontal);
			}
		}
		wallMatrixY = new WallPO[width+1][height];
		for(int i=0;i<width+1;i++){
			for(int j=0;j<height;j++){
				wallMatrixY[i][j]=new WallPO(WallState.black,i,j,WallDirection.virtical);
			}
		}//实例化墙
		for(int i=0;i<width;i++){
			wallMatrixX[i][height].setState(WallState.red);
			wallMatrixX[i][0].setState(WallState.red);
		}
		for(int i=0;i<height;i++){
			wallMatrixY[0][i].setState(WallState.red);
			wallMatrixY[width][i].setState(WallState.red);
		}//初始化边界
		for(int i=0;i<playerNum;i++){
			playerMatrix[i].setPlayNo(i);
			playerMatrix[i].setWallLeft(this.wallNum);
		}//初始化玩家剩余墙数
		for(int i=0;i<width;i++){
			for(int j=0;j<height;j++){
				blockMatrix[i][j].setX(i);
				blockMatrix[i][j].setY(j);
				blockMatrix[i][j].setState(BlockState.empty);
			}
		}//初始化网格状态
		detailInit(height,width,playerNum);//初始化细节
		return true;
	}

	private void detailInit(int height,int width,int playerNum){
		if(playerNum==2){
			blockMatrix[width/2][0].setState(BlockState.red);//红色在最下方
			blockMatrix[width/2][height-1].setState(BlockState.blue);//蓝色在最上方
		}
		if(playerNum>=3){
			blockMatrix[width/2][0].setState(BlockState.red);//红色在最下方
			blockMatrix[0][height/2].setState(BlockState.yellow);//黄色在最左方
			blockMatrix[width/2][height-1].setState(BlockState.blue);//蓝色在最上方
			if(playerNum==4){
				blockMatrix[width-1][height/2].setState(BlockState.green);//绿色在最右方
			}
		}
		//玩家一至四颜色对应red,blue,yellow,green
	}
	
	
	@Override
	public void setGameModel(GameModelService gameModel) {
		this.gameModel = gameModel;
		// TODO Auto-generated method stub
	}

	@Override
	public boolean move(int playerNo,Direction direction) {
		// TODO Auto-generated method stub
		boolean result =false;
		int x=0;
		int y=0;
		BlockState bs=getPlayerColor(playerNo);//获取棋子颜色
		for(int i=0;i<width;i++){
			for(int j=0;j<height;j++){
				if(blockMatrix[i][j].getState()==bs){
					x=i;
					y=j;
				}
			}
		}//获取棋子坐标
		switch(direction){//根据移动方向判断是否可以移动，步骤分两步，1.移动方向上是否有墙（边界视作墙），2.移动方向上有几个棋子
		case up:
			if(wallMatrixX[x][y+1].getState()==WallState.black){//判断移动方向上是否有墙
				if(blockMatrix[x][y+1].getState()==BlockState.empty){//移动方向上没墙切移动方向上没子
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x][y+1].setState(bs);
					y++;//存放移动后坐标，便于判断胜负
					result=true;
				}
				else if(wallMatrixX[x][y+2].getState()==WallState.black
						&&blockMatrix[x][y+2].getState()==BlockState.empty){//如果仅有一子且该子上方也没有墙
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x][y+2].setState(bs);
					y+=2;//存放移动后坐标，便于判断胜负
					result=true;
				}
				if(result==true&&bs==BlockState.red&&y==height-1){//判断胜负：上移成功、子为红色、且到达顶端
					this.gameModel.gameOver(GameResultState.RedWin);
				}
			}
			break;
		case down:
			if(wallMatrixX[x][y].getState()==WallState.black){
				if(blockMatrix[x][y-1].getState()==BlockState.empty){
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x][y-1].setState(bs);
					y--;
					result=true;
				}
				else if(wallMatrixX[x][y-1].getState()==WallState.black
						&&blockMatrix[x][y-2].getState()==BlockState.empty){
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x][y-2].setState(bs);
					y-=2;
					result=true;
				}
				if(result==true&&bs==BlockState.blue&&y==0){
					this.gameModel.gameOver(GameResultState.BlueWin);
				}
			}
			break;
		case left:
			if(wallMatrixY[x][y].getState()==WallState.black){
				if(blockMatrix[x-1][y].getState()==BlockState.empty){
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x-1][y].setState(bs);
					x--;
					result=true;
				}
				else if(wallMatrixY[x-1][y].getState()==WallState.black
						&&blockMatrix[x-2][y].getState()==BlockState.empty){
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x-2][y].setState(bs);
					x-=2;
					result=true;
				}
				if(result==true&&bs==BlockState.green&&x==0){
					this.gameModel.gameOver(GameResultState.GreenWin);
				}
				
			}
			break;
		case right:
			if(wallMatrixY[x+1][y].getState()==WallState.black){
				if(blockMatrix[x+1][y].getState()==BlockState.empty){
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x+1][y].setState(bs);
					x++;
					result=true;
				}
				else if(wallMatrixY[x+2][y].getState()==WallState.black
						&&blockMatrix[x+2][y].getState()==BlockState.empty){
					blockMatrix[x][y].setState(BlockState.empty);
					blockMatrix[x+2][y].setState(bs);
					x+=2;
					result=true;
				}
			}
			if(result==true&&bs==BlockState.yellow&&x==width-1){
				this.gameModel.gameOver(GameResultState.YellowWin);
			}
			break;
		default:
			break; 
		}
		List<BlockPO> blocks=new ArrayList<BlockPO>();
		for(int i=0;i<width;i++){
			for(int j=0;j<height;j++){
				if(blockMatrix[i][j].getState()!=BlockState.empty){
					blocks.add(blockMatrix[i][j]);
				}
				
			}
		}
		super.updateChange(new UpdateMessage("move",this.getBlockDisplayList(blocks)));	
		return result;
	}
	
	private List<BlockVO> getBlockDisplayList(List<BlockPO> blocks){
		List<BlockVO> result = new ArrayList<BlockVO>();
		for(BlockPO block:blocks){
			result.add(block.getDisplayBlock());
		}	
		return result;
	}
	
	
	private BlockState getPlayerColor(int playerNo){
		BlockState bs=null;
		if(playerNo==1){
			bs=BlockState.red;
		}
		else if(playerNo==2){
			bs=BlockState.blue;
		}
		else if(playerNo==3){
			bs=BlockState.yellow;
		}
		else if(playerNo==4){
			bs=BlockState.green;
		}
		return bs;
	}

	@Override
	public boolean set(int playerNo,int x, int y, WallDirection wallDirection) {//设墙判断分两步，是否与其他墙重叠(包含是否与边线重叠)，是否圈死棋子
		if(playerMatrix[playerNo-1].getWallLeft()<=0){
			return false;
		}
		if(x==0||y==0||x==width||y==height){
			return false;
		}
		if(wallDirection==WallDirection.horizontal){
			if(wallMatrixX[x][y].getState()==WallState.black||wallMatrixX[x][y-1].getState()==WallState.black){
				if(wallMatrixY[x][y].getState()==WallState.black&&wallMatrixY[x-1][y].getState()==WallState.black){
					wallMatrixY[x][y].setState(WallState.red);
					wallMatrixY[x-1][y].setState(WallState.red);
					return true;
				}
			}
		}
		else{
			if(wallMatrixY[x][y].getState()==WallState.black||wallMatrixY[x-1][y].getState()==WallState.black){
				if(wallMatrixX[x][y].getState()==WallState.black&&wallMatrixX[x][y-1].getState()==WallState.black){
					wallMatrixX[x][y].setState(WallState.red);
					wallMatrixX[x][y-1].setState(WallState.red);
					return true;
				}
			}
		}
		List<WallPO> walls=new ArrayList<WallPO>();
		for(int i=0;i<width;i++){
			for(int j=0;j<height+1;j++){
				if(wallMatrixX[i][j].getState()==WallState.red){
					walls.add(wallMatrixX[i][j]);
				}
			}
		}
		for(int i=0;i<width+1;i++){
			for(int j=0;j<height;j++){
				if(wallMatrixY[i][j].getState()==WallState.red){
					walls.add(wallMatrixY[i][j]);
				}
			}
		}
		super.updateChange(new UpdateMessage("set",this.getWallDisplayList(walls)));
		return false;
	}//暂时没考虑成环问题
	
	private List<WallVO> getWallDisplayList(List<WallPO> walls){
		List<WallVO> result=new ArrayList<WallVO>();
		for(WallPO wall:walls){
			result.add(wall.getDisplayWall());
		}
		return result;
	}
	
	
	public void wallPrint(){
		for(int i=8;i>=0;i--){
			for(int j=0;j<9;j++){
				switch(blockMatrix[j][i].getState()){
				case blue:
					System.out.print(2+" ");
					break;
				case empty:
					System.out.print(0+" ");
					break;
				case green:
					System.out.print(4+" ");
					break;
				case red:
					System.out.print(1+" ");
					break;
				case yellow:
					System.out.print(3+" ");
					break;
				default:
					break;
				}
			}
			System.out.println();
		}
	}
}
