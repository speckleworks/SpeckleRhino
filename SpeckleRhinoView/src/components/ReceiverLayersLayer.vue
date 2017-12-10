<template>
  <v-layout @mouseover='mouseOver' @mouseleave='mouseOut' align-center>
    <v-flex>
      <v-btn icon small xs flat @click.native='visible=!visible' color='grey'>
        <v-icon dark>{{ visible ? "visibility" : "visibility_off" }}</v-icon>
      </v-btn>
      <v-btn icon small xs flat @click.native='bake' color='grey'>
        <v-icon dark>play_for_work</v-icon>
      </v-btn>
    </v-flex>
    <v-flex xs6 class='layername'>
      <span class='subheading'>{{ layer.name }}</span>
      <span class="caption grey--text"> Object count: {{layer.objectCount }} </span>
    </v-flex>
    <v-flex style='text-align: center;'>
      <v-icon dark :style='{ color: layerColor.hex }' class='make-me-small'>fiber_manual_record</v-icon>
    </v-flex>
  </v-layout>
</template>
<script>
export default {
  name: '',
  props: {
    layer: Object,
    clientId: String
  },
  components: {},
  computed: {
    layerColor( ) {
      if ( this.layer.properties && this.layer.properties.color )
        return this.layer.properties.color
      return { hex: '#AEECFD', alpha: 1 }
    }
  },
  watch: {
    visible( value ) {
      console.log( this.clientId, this.layer.guid, value )
      Interop.setLayerVisibility( this.clientId, this.layer.guid, value )
    }
  },
  data( ) {
    return {
      visible: true
    }
  },
  methods: {
    mouseOver( ) {
      Interop.setLayerHover( this.clientId, this.layer.guid, true )
    },
    mouseOut( ) {
      Interop.setLayerHover( this.clientId, this.layer.guid, false )
    },
    bake( ) {
      Interop.bakeLayer( this.clientId, this.layer.guid )
    }
  },
  mounted( ) {}
}
</script>
<style lang='scss'>
.make-me-small {
  font-size: 14px !important;
}

.layername {
  text-overflow: ellipsis;
  /* Required for text-overflow to do anything */
  white-space: nowrap;
  overflow: hidden;
}
</style>